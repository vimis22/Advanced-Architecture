using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using UnifiedScheduler.Models;

namespace UnifiedScheduler.Services;

public class HeartbeatObserver
{
    private readonly IMqttClient _mqttClient;
    private readonly RedisService _redis;
    private readonly TimescaleDBService _timescale;
    private readonly JobQueueManager _queueManager;

    // Track previous state to detect transitions
    private readonly Dictionary<string, (string status, string? unitId, int? progress)> _previousState = new();

    public HeartbeatObserver(
        IMqttClient mqttClient,
        RedisService redis,
        TimescaleDBService timescale,
        JobQueueManager queueManager)
    {
        _mqttClient = mqttClient;
        _redis = redis;
        _timescale = timescale;
        _queueManager = queueManager;
    }

    public async Task StartAsync()
    {
        // Subscribe to all machine heartbeats
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("machines/+/heartbeat")
            .Build();

        await _mqttClient.SubscribeAsync(subscribeOptions);

        Console.WriteLine("[HeartbeatObserver] Started listening to machine heartbeats");
    }

    public async Task OnHeartbeatReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            var heartbeat = JsonConvert.DeserializeObject<HeartbeatMessage>(payload);

            if (heartbeat == null || string.IsNullOrEmpty(heartbeat.MachineId))
            {
                Console.WriteLine("[HeartbeatObserver] Invalid heartbeat message");
                return;
            }

            // VALIDATION: If machine reports "running" with a unit, verify it's actually assigned to that unit
            if (heartbeat.Status == "running" && !string.IsNullOrEmpty(heartbeat.CurrentUnitId))
            {
                var isValid = await ValidateUnitAssignment(heartbeat.MachineId, heartbeat.CurrentUnitId, heartbeat.MachineType);
                if (!isValid)
                {
                    Console.WriteLine($"[HeartbeatObserver] ⚠ INVALID ASSIGNMENT: Machine {heartbeat.MachineId} claims to be processing {heartbeat.CurrentUnitId}, but it's not assigned to it. Ignoring heartbeat.");
                    return; // Drop this heartbeat - machine is processing a stale assignment
                }
            }

            // Update Redis with current machine state
            await _redis.UpdateMachineStateAsync(heartbeat);

            // Detect job completion and state changes
            await DetectJobCompletionAsync(heartbeat);

            //Console.WriteLine($"[HeartbeatObserver] {heartbeat.MachineId} ({heartbeat.MachineType}): " +
            //                $"status={heartbeat.Status}, unit={heartbeat.CurrentUnitId ?? "none"}, " +
            //                $"progress={heartbeat.Progress?.ToString() ?? "N/A"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HeartbeatObserver] Error processing heartbeat: {ex.Message}");
        }
    }

    private async Task<bool> ValidateUnitAssignment(string machineId, string unitId, string machineType)
    {
        // Get unit state from Redis
        var unitState = await _redis.GetUnitStateAsync(unitId);
        if (unitState.Count == 0)
        {
            // Unit doesn't exist in Redis - likely already completed and cleaned up
            return false;
        }

        // Determine which job type this machine handles
        var jobType = machineType.ToUpper() switch
        {
            "A" => "job_a",
            "B" => "job_b",
            "C" => "job_c",
            "D" => "job_d",
            _ => null
        };

        if (jobType == null)
            return false;

        // Check if this machine is actually assigned to this unit for this job type
        var assignedMachine = unitState.GetValueOrDefault($"{jobType}_machine", "");
        var jobStatus = unitState.GetValueOrDefault($"{jobType}_status", "");

        // CRITICAL: Reject if job is already completed
        if (jobStatus == "completed")
        {
            Console.WriteLine($"[HeartbeatObserver] ⚠ Job {jobType} for unit {unitId} is ALREADY COMPLETED. Machine {machineId} should not be processing it.");
            return false;
        }

        // Valid if:
        // 1. This machine is assigned to the unit AND
        // 2. The job status is "running" (not pending, not completed, not waiting)
        var isValid = assignedMachine == machineId && jobStatus == "running";

        if (!isValid && jobStatus == "running")
        {
            Console.WriteLine($"[HeartbeatObserver] ⚠ Unit {unitId} {jobType} is assigned to {assignedMachine}, but {machineId} is trying to process it.");
        }

        return isValid;
    }

    private async Task DetectJobCompletionAsync(HeartbeatMessage heartbeat)
    {
        var machineId = heartbeat.MachineId;
        var currentState = (heartbeat.Status, heartbeat.CurrentUnitId, heartbeat.Progress);

        // Get previous state
        if (!_previousState.TryGetValue(machineId, out var previousState))
        {
            // First time seeing this machine
            _previousState[machineId] = currentState;
            return;
        }

        // Detect job completion: progress reached 100 or status changed from running to idle with a unit
        bool jobCompleted = false;

        if (previousState.status == "running" && heartbeat.Status == "running" &&
            previousState.unitId == heartbeat.CurrentUnitId &&
            heartbeat.Progress == 100)
        {
            // Job completed (progress = 100)
            jobCompleted = true;
        }
        else if (previousState.status == "running" && heartbeat.Status == "idle" &&
                 !string.IsNullOrEmpty(previousState.unitId))
        {
            // Job completed (transitioned to idle from running)
            jobCompleted = true;
            heartbeat.CurrentUnitId = previousState.unitId; // Use previous unit ID
        }

        if (jobCompleted && !string.IsNullOrEmpty(heartbeat.CurrentUnitId))
        {
            Console.WriteLine($"[HeartbeatObserver] ✓ Job completed: {heartbeat.CurrentUnitId} on {machineId}");
            await HandleJobCompletionAsync(heartbeat);
        }

        // Update previous state
        _previousState[machineId] = currentState;
    }

    private async Task HandleJobCompletionAsync(HeartbeatMessage heartbeat)
    {
        if (string.IsNullOrEmpty(heartbeat.CurrentUnitId))
            return;

        var unitId = heartbeat.CurrentUnitId;

        // Determine job type from machine type
        var jobType = heartbeat.MachineType.ToUpper() switch
        {
            "A" => "job_a",
            "B" => "job_b",
            "C" => "job_c",
            "D" => "job_d",
            _ => null
        };

        if (jobType == null)
        {
            Console.WriteLine($"[HeartbeatObserver] Unknown machine type: {heartbeat.MachineType}");
            return;
        }

        // Update unit status in Redis (Redis is source of truth for unit state)
        await _redis.UpdateUnitJobAsync(unitId, jobType, "completed", heartbeat.MachineId);

        // Check dependencies and re-queue for next job
        await RequeueToNextJobAsync(unitId, jobType);

        Console.WriteLine($"[HeartbeatObserver] Updated {unitId}: {jobType} completed by {heartbeat.MachineId}");
    }

    private async Task RequeueToNextJobAsync(string unitId, string completedJobType)
    {
        // Get unit state from Redis
        var unitState = await _redis.GetUnitStateAsync(unitId);

        if (unitState.Count == 0)
        {
            Console.WriteLine($"[HeartbeatObserver] Unit {unitId} not found in Redis");
            return;
        }

        var jobAStatus = unitState.GetValueOrDefault("job_a_status", "");
        var jobBStatus = unitState.GetValueOrDefault("job_b_status", "");
        var jobCStatus = unitState.GetValueOrDefault("job_c_status", "");
        var jobDStatus = unitState.GetValueOrDefault("job_d_status", "");

        // Dependency logic: A & B -> C -> D
        if (completedJobType == "job_a" || completedJobType == "job_b")
        {
            // Check if BOTH job_a and job_b are completed
            if (jobAStatus == "completed" && jobBStatus == "completed" && jobCStatus == "waiting")
            {
                // Ready for job_c (binding) - use MEDIUM priority (2) because dependencies are met
                await _redis.UpdateUnitJobAsync(unitId, "job_c", "pending");
                await _queueManager.EnqueueJobAsync("job_c", unitId, priority: 2);

                Console.WriteLine($"[HeartbeatObserver] → Unit {unitId} queued for job_c (binding) with MEDIUM priority");
            }
        }
        else if (completedJobType == "job_c")
        {
            // job_c completed, ready for job_d (packaging)
            if (jobDStatus == "waiting")
            {
                await _redis.UpdateUnitJobAsync(unitId, "job_d", "pending");
                await _queueManager.EnqueueJobAsync("job_d", unitId);

                Console.WriteLine($"[HeartbeatObserver] → Unit {unitId} queued for job_d (packaging)");
            }
        }
        else if (completedJobType == "job_d")
        {
            // All jobs completed for this unit!
            var orderIdStr = unitState.GetValueOrDefault("order_id", "");
            if (int.TryParse(orderIdStr, out var orderId))
            {
                // Increment completed units counter in Redis
                await _redis.IncrementCompletedUnitsAsync(orderId);
                await _redis.DeleteUnitStateAsync(unitId); // Cleanup unit state

                // Get order info to check if all units are completed
                var order = await _timescale.GetOrderAsync(orderId);
                var completedCount = await _redis.GetCompletedUnitsCountAsync(orderId);

                if (order != null && completedCount >= order.Quantity)
                {
                    // All units for this order are completed - mark order as completed in TimescaleDB
                    await _timescale.MarkOrderCompletedAsync(orderId);

                    // Cleanup: Remove the completed units counter from Redis (full cleanup)
                    await _redis.DeleteOrderTrackingAsync(orderId);

                    // Small delay to ensure database commit completes
                    await Task.Delay(100);

                    // Get updated order info with completion time
                    var completedOrder = await _timescale.GetOrderAsync(orderId);

                    // Get requeue statistics for this order
                    var requeueStats = await _timescale.GetOrderRequeueStatsAsync(orderId);

                    // Prepare statistics message
                    string statsMessage;
                    if (completedOrder?.StartedAt != null && completedOrder?.CompletedAt != null)
                    {
                        var startTime = completedOrder.StartedAt.Value.ToLocalTime();
                        var endTime = completedOrder.CompletedAt.Value.ToLocalTime();
                        var duration = endTime - startTime;

                        if (requeueStats.TotalRecoveries > 0)
                        {
                            statsMessage = $"★ ORDER {orderId} COMPLETED ★ | {order.Title} | " +
                                         $"{completedCount}/{order.Quantity} units | " +
                                         $"Duration: {duration.TotalMinutes:F2} min ({duration.TotalSeconds:F1}s) | " +
                                         $"Requeues: {requeueStats.TotalRecoveries} | " +
                                         $"Avg Recovery: {requeueStats.AvgRecoveryMs:F1}ms | " +
                                         $"Start: {startTime:HH:mm:ss} | End: {endTime:HH:mm:ss}";
                        }
                        else
                        {
                            statsMessage = $"★ ORDER {orderId} COMPLETED ★ | {order.Title} | " +
                                         $"{completedCount}/{order.Quantity} units | " +
                                         $"Duration: {duration.TotalMinutes:F2} min ({duration.TotalSeconds:F1}s) | " +
                                         $"No failures - completed without requeues! | " +
                                         $"Start: {startTime:HH:mm:ss} | End: {endTime:HH:mm:ss}";
                        }
                    }
                    else
                    {
                        statsMessage = $"★ ORDER {orderId} COMPLETED ★ | {order.Title} | {completedCount}/{order.Quantity} units";
                    }

                    // Publish to MQTT dashboard
                    var completionPayload = new
                    {
                        order_id = orderId,
                        title = order.Title,
                        units = completedCount,
                        total = order.Quantity,
                        duration_minutes = completedOrder?.StartedAt != null && completedOrder?.CompletedAt != null
                            ? (completedOrder.CompletedAt.Value - completedOrder.StartedAt.Value).TotalMinutes
                            : 0,
                        requeues = requeueStats.TotalRecoveries,
                        avg_recovery_ms = requeueStats.AvgRecoveryMs,
                        message = statsMessage
                    };

                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(completionPayload);
                    var mqttMessage = new MQTTnet.MqttApplicationMessageBuilder()
                        .WithTopic("scheduler/order/completed")
                        .WithPayload(json)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await _mqttClient.PublishAsync(mqttMessage);

                    // Also log to console
                    Console.WriteLine($"\n{'=',60}");
                    Console.WriteLine(statsMessage);
                    Console.WriteLine($"{'=',60}\n");
                }
                else
                {
                    Console.WriteLine($"[HeartbeatObserver] ✓ Unit {unitId} FULLY COMPLETED ({completedCount}/{order?.Quantity} units done)");
                }
            }
        }
    }
}
