using StackExchange.Redis;
using Newtonsoft.Json;
using UnifiedScheduler.Models;

namespace UnifiedScheduler.Services;

public class RedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    // ============ Machine State Management ============

    public async Task UpdateMachineStateAsync(HeartbeatMessage heartbeat)
    {
        var key = $"machine:{heartbeat.MachineId}";
        var entries = new HashEntry[]
        {
            new HashEntry("machine_type", heartbeat.MachineType),
            new HashEntry("status", heartbeat.Status),
            new HashEntry("current_unit_id", heartbeat.CurrentUnitId ?? ""),
            new HashEntry("progress", heartbeat.Progress?.ToString() ?? ""),
            new HashEntry("last_heartbeat", heartbeat.Timestamp.ToString("o"))
        };

        await _db.HashSetAsync(key, entries);
    }

    public async Task<MachineState?> GetMachineStateAsync(string machineId)
    {
        var key = $"machine:{machineId}";
        var entries = await _db.HashGetAllAsync(key);

        if (entries.Length == 0)
            return null;

        var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

        return new MachineState
        {
            MachineId = machineId,
            MachineType = dict.GetValueOrDefault("machine_type", ""),
            Status = dict.GetValueOrDefault("status", "off"),
            CurrentUnitId = string.IsNullOrEmpty(dict.GetValueOrDefault("current_unit_id", "")) ? null : dict["current_unit_id"],
            Progress = int.TryParse(dict.GetValueOrDefault("progress", ""), out var p) ? p : null,
            LastHeartbeat = DateTime.TryParse(dict.GetValueOrDefault("last_heartbeat", ""), out var dt) ? dt : DateTime.MinValue
        };
    }

    public Task<List<string>> GetAllMachineIdsAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: "machine:*").Select(k => k.ToString().Replace("machine:", "")).ToList();
        return Task.FromResult(keys);
    }

    public async Task<List<MachineState>> GetAvailableMachinesAsync(string machineType)
    {
        var allMachineIds = await GetAllMachineIdsAsync();
        var machines = new List<MachineState>();

        foreach (var machineId in allMachineIds)
        {
            var state = await GetMachineStateAsync(machineId);
            if (state != null && state.MachineType == machineType && state.Status == "idle")
            {
                machines.Add(state);
            }
        }

        return machines;
    }

    // ============ Job Queue Management (Priority-Based) ============
    // Priority levels:
    // 3 = Highest (re-queued jobs from failures)
    // 2 = Medium (jobs with dependencies completed, ready to process)
    // 1 = Lowest (new jobs just created)

    public async Task EnqueueJobAsync(string jobType, string unitId, int priority = 1)
    {
        var queueKey = $"job_queue_{jobType}";

        // Higher score = higher priority (dequeue from highest score first)
        // Use priority as major component, timestamp as tiebreaker to maintain FIFO within same priority
        // Score format: priority * 1,000,000,000,000 + (max_timestamp - current_timestamp)
        // This ensures higher priority always wins, and within same priority, older items are processed first
        var timestamp = DateTime.UtcNow.Ticks;
        var maxTimestamp = DateTime.MaxValue.Ticks;
        var score = (double)(priority * 1_000_000_000_000L + (maxTimestamp - timestamp));

        await _db.SortedSetAddAsync(queueKey, unitId, score);
    }

    public async Task<string?> DequeueJobAsync(string jobType)
    {
        var queueKey = $"job_queue_{jobType}";

        // Get highest priority item (highest score)
        var items = await _db.SortedSetRangeByScoreAsync(queueKey, order: StackExchange.Redis.Order.Descending, take: 1);

        if (items.Length == 0)
            return null;

        var unitId = items[0].ToString();
        if (!string.IsNullOrEmpty(unitId))
        {
            await _db.SortedSetRemoveAsync(queueKey, unitId);
        }
        return string.IsNullOrEmpty(unitId) ? null : unitId;
    }

    public async Task<long> GetQueueLengthAsync(string jobType)
    {
        var queueKey = $"job_queue_{jobType}";
        return await _db.SortedSetLengthAsync(queueKey);
    }

    // ============ Unit State Management ============
    // Redis is the SOURCE OF TRUTH for real-time unit and machine state

    public async Task SetUnitStateAsync(string unitId, string field, string value)
    {
        var key = $"unit:{unitId}";
        await _db.HashSetAsync(key, field, value);
    }

    public async Task<Dictionary<string, string>> GetUnitStateAsync(string unitId)
    {
        var key = $"unit:{unitId}";
        var entries = await _db.HashGetAllAsync(key);
        return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
    }

    public async Task DeleteUnitStateAsync(string unitId)
    {
        var key = $"unit:{unitId}";
        await _db.KeyDeleteAsync(key);
    }

    public async Task InitializeUnitAsync(string unitId, int orderId, int unitNumber)
    {
        var key = $"unit:{unitId}";
        var entries = new HashEntry[]
        {
            new HashEntry("order_id", orderId.ToString()),
            new HashEntry("unit_number", unitNumber.ToString()),
            new HashEntry("job_a_status", "pending"),
            new HashEntry("job_b_status", "pending"),
            new HashEntry("job_c_status", "waiting"),
            new HashEntry("job_d_status", "waiting"),
            new HashEntry("job_a_machine", ""),
            new HashEntry("job_b_machine", ""),
            new HashEntry("job_c_machine", ""),
            new HashEntry("job_d_machine", "")
        };

        await _db.HashSetAsync(key, entries);
    }

    // Update unit job status with timestamps
    public async Task UpdateUnitJobAsync(string unitId, string jobType, string status, string? machineId = null)
    {
        var key = $"unit:{unitId}";
        var updates = new List<HashEntry>
        {
            new HashEntry($"{jobType}_status", status)
        };

        if (machineId != null)
        {
            updates.Add(new HashEntry($"{jobType}_machine", machineId));
        }

        if (status == "running")
        {
            updates.Add(new HashEntry($"{jobType}_started_at", DateTime.UtcNow.ToString("o")));
        }
        else if (status == "completed")
        {
            updates.Add(new HashEntry($"{jobType}_completed_at", DateTime.UtcNow.ToString("o")));
        }

        await _db.HashSetAsync(key, updates.ToArray());
    }

    // Get all units for an order
    public async Task<List<string>> GetOrderUnitIdsAsync(int orderId)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var pattern = $"unit:{orderId}:*";
        var keys = server.Keys(pattern: pattern).Select(k => k.ToString().Replace("unit:", "")).ToList();
        return keys;
    }

    // Get all units currently assigned to a machine (for orphan detection)
    public async Task<List<(string UnitId, string JobType)>> GetUnitsOnMachineAsync(string machineId, string jobType)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var allUnitKeys = server.Keys(pattern: "unit:*").ToList();
        var results = new List<(string, string)>();

        foreach (var key in allUnitKeys)
        {
            var unitId = key.ToString().Replace("unit:", "");
            var state = await GetUnitStateAsync(unitId);

            if (state.TryGetValue($"{jobType}_machine", out var assignedMachine) &&
                assignedMachine == machineId &&
                state.TryGetValue($"{jobType}_status", out var status) &&
                status == "running")
            {
                results.Add((unitId, jobType));
            }
        }

        return results;
    }

    // Get all orphaned units (running >60s or machine not in active list)
    public async Task<List<(string UnitId, string JobType, string MachineId, DateTime StartedAt)>> GetOrphanedUnitsAsync(List<string> activeMachineIds)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var allUnitKeys = server.Keys(pattern: "unit:*").ToList();
        var results = new List<(string, string, string, DateTime)>();
        var jobTypes = new[] { "job_a", "job_b", "job_c", "job_d" };

        foreach (var key in allUnitKeys)
        {
            var unitId = key.ToString().Replace("unit:", "");
            var state = await GetUnitStateAsync(unitId);

            foreach (var jobType in jobTypes)
            {
                if (state.TryGetValue($"{jobType}_status", out var status) && status == "running" &&
                    state.TryGetValue($"{jobType}_machine", out var machineId) && !string.IsNullOrEmpty(machineId))
                {
                    var isOrphaned = false;
                    DateTime startedAt = DateTime.UtcNow;

                    // Check if machine is no longer active
                    if (!activeMachineIds.Contains(machineId))
                    {
                        isOrphaned = true;
                    }
                    // Check if running too long (>60 seconds)
                    else if (state.TryGetValue($"{jobType}_started_at", out var startedAtStr) &&
                             DateTime.TryParse(startedAtStr, out startedAt))
                    {
                        var runningSince = DateTime.UtcNow - startedAt;
                        if (runningSince.TotalSeconds > 60)
                        {
                            isOrphaned = true;
                        }
                    }

                    if (isOrphaned)
                    {
                        results.Add((unitId, jobType, machineId, startedAt));
                    }
                }
            }
        }

        return results;
    }

    // ============ Order Tracking ============

    public async Task IncrementCompletedUnitsAsync(int orderId)
    {
        var key = $"order:{orderId}:completed_units";
        await _db.StringIncrementAsync(key);
    }

    public async Task<long> GetCompletedUnitsCountAsync(int orderId)
    {
        var key = $"order:{orderId}:completed_units";
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? 0 : (long)value;
    }

    public async Task DeleteOrderTrackingAsync(int orderId)
    {
        var key = $"order:{orderId}:completed_units";
        await _db.KeyDeleteAsync(key);
    }
}
