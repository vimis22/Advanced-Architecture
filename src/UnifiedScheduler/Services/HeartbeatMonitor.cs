using UnifiedScheduler.Models;

namespace UnifiedScheduler.Services;

public class HeartbeatMonitor
{
    private readonly RedisService _redis;
    private readonly TimescaleDBService _timescale;
    private readonly JobQueueManager _queueManager;
    private readonly int _heartbeatIntervalSeconds;
    private readonly int _timeoutCycles;
    private CancellationTokenSource? _cts;

    public HeartbeatMonitor(
        RedisService redis,
        TimescaleDBService timescale,
        JobQueueManager queueManager,
        int heartbeatIntervalSeconds,
        int timeoutCycles)
    {
        _redis = redis;
        _timescale = timescale;
        _queueManager = queueManager;
        _heartbeatIntervalSeconds = heartbeatIntervalSeconds;
        _timeoutCycles = timeoutCycles;
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Console.WriteLine($"[HeartbeatMonitor] Started monitoring (timeout: {_timeoutCycles} cycles = {_timeoutCycles * _heartbeatIntervalSeconds}s)");

        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await CheckMachineHealthAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HeartbeatMonitor] ERROR: {ex.Message}");
                    Console.WriteLine($"[HeartbeatMonitor] Stack trace: {ex.StackTrace}");
                }

                await Task.Delay(_heartbeatIntervalSeconds * 1000, token);
            }
        }, token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        Console.WriteLine("[HeartbeatMonitor] Stopped");
    }

    private async Task CheckMachineHealthAsync()
    {
        var machineIds = await _redis.GetAllMachineIdsAsync();
        var now = DateTime.UtcNow;
        var timeoutThreshold = TimeSpan.FromSeconds(_heartbeatIntervalSeconds * _timeoutCycles);

        foreach (var machineId in machineIds)
        {
            var state = await _redis.GetMachineStateAsync(machineId);
            if (state == null)
                continue;

            var timeSinceLastHeartbeat = now - state.LastHeartbeat;

            if (timeSinceLastHeartbeat > timeoutThreshold && state.Status != "off")
            {
                Console.WriteLine($"[HeartbeatMonitor] ⚠ MACHINE FAILURE DETECTED: {machineId} " +
                                $"(last seen {timeSinceLastHeartbeat.TotalSeconds:F1}s ago)");

                // Handle machine failure - this will requeue units on THIS machine
                await HandleMachineFailureAsync(state);
            }
        }

        // NOTE: We don't run CheckOrphanedUnitsAsync here anymore!
        // Units are requeued immediately when HandleMachineFailureAsync detects a failure.
        // The only units that might get stuck are those where the machine restarted so fast
        // that it's still in the active list, which is prevented by the 30s restart delay.
    }

    private async Task CheckOrphanedUnitsAsync(List<string> activeMachineIds)
    {
        try
        {
            // Use Redis to find orphaned units (source of truth for unit state)
            var orphanedUnits = await _redis.GetOrphanedUnitsAsync(activeMachineIds);

            if (orphanedUnits.Count > 0)
            {
                Console.WriteLine($"[HeartbeatMonitor] Found {orphanedUnits.Count} orphaned unit(s)");

                foreach (var (unitId, jobType, machineId, startedAt) in orphanedUnits)
                {
                    var detectedAt = DateTime.UtcNow;
                    Console.WriteLine($"[HeartbeatMonitor] Re-queuing orphaned unit {unitId} ({jobType}) from machine {machineId}");

                    // Get machine type and order ID from unit state
                    var machineType = jobType.Replace("job_", "").ToUpper();
                    var unitState = await _redis.GetUnitStateAsync(unitId);
                    var orderId = int.Parse(unitState.GetValueOrDefault("order_id", "0"));

                    // Reset unit status back to pending in Redis
                    await _redis.UpdateUnitJobAsync(unitId, jobType, "pending", machineId: "");

                    // Re-queue the job with HIGHEST priority (3) for failure recovery
                    await _queueManager.EnqueueJobAsync(jobType, unitId, priority: 3);

                    var requeuedAt = DateTime.UtcNow;
                    var recoveryMs = (int)(requeuedAt - detectedAt).TotalMilliseconds;

                    // Log requeue event to TimescaleDB for analytics
                    await _timescale.LogRequeueEventAsync(
                        unitId,
                        orderId,
                        jobType,
                        machineId,
                        machineType,
                        "orphaned",
                        detectedAt,
                        requeuedAt
                    );

                    Console.WriteLine($"[HeartbeatMonitor] ✓ Orphaned unit {unitId} re-queued to {jobType} with HIGH priority (recovery time: {recoveryMs}ms)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HeartbeatMonitor] ERROR checking orphaned units: {ex.Message}");
        }
    }

    private async Task HandleMachineFailureAsync(MachineState machine)
    {
        try
        {
            var failureDetectedAt = DateTime.UtcNow;
            Console.WriteLine($"[HeartbeatMonitor] Machine failure detected: {machine.MachineId} (type: {machine.MachineType})");

            // Query Redis for ALL units stuck in "running" state for this machine
            var jobTypes = new[] { "job_a", "job_b", "job_c", "job_d" };
            var runningUnits = new List<(string UnitId, string JobType)>();

            foreach (var jobType in jobTypes)
            {
                if (jobType.Contains(machine.MachineType.ToLower()))
                {
                    var units = await _redis.GetUnitsOnMachineAsync(machine.MachineId, jobType);
                    runningUnits.AddRange(units);
                }
            }

            if (runningUnits.Count > 0)
            {
                Console.WriteLine($"[HeartbeatMonitor] Found {runningUnits.Count} running unit(s) on failed machine {machine.MachineId}");

                foreach (var (unitId, jobType) in runningUnits)
                {
                    Console.WriteLine($"[HeartbeatMonitor] Re-queuing unit {unitId} ({jobType}) from failed machine {machine.MachineId}");

                    // Get order ID from unit state
                    var unitState = await _redis.GetUnitStateAsync(unitId);
                    var orderId = int.Parse(unitState.GetValueOrDefault("order_id", "0"));

                    // Reset unit status back to pending in Redis
                    await _redis.UpdateUnitJobAsync(unitId, jobType, "pending", machineId: "");

                    // Re-queue the job with HIGHEST priority (3) for failure recovery
                    await _queueManager.EnqueueJobAsync(jobType, unitId, priority: 3);

                    var requeuedAt = DateTime.UtcNow;
                    var recoveryMs = (int)(requeuedAt - failureDetectedAt).TotalMilliseconds;

                    // Log requeue event to TimescaleDB for analytics
                    await _timescale.LogRequeueEventAsync(
                        unitId,
                        orderId,
                        jobType,
                        machine.MachineId,
                        machine.MachineType,
                        "machine_failure",
                        failureDetectedAt,
                        requeuedAt
                    );

                    Console.WriteLine($"[HeartbeatMonitor] ✓ Unit {unitId} re-queued to {jobType} with HIGH priority (recovery time: {recoveryMs}ms)");
                }
            }
            else
            {
                Console.WriteLine($"[HeartbeatMonitor] No running units found for failed machine {machine.MachineId}");
            }

            Console.WriteLine($"[HeartbeatMonitor] Machine {machine.MachineId} marked as FAILED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HeartbeatMonitor] ERROR in HandleMachineFailureAsync for {machine.MachineId}: {ex.Message}");
            Console.WriteLine($"[HeartbeatMonitor] Stack trace: {ex.StackTrace}");
        }
    }
}
