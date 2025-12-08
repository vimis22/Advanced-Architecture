using MQTTnet.Client;
using MQTTnet;
using Newtonsoft.Json;
using UnifiedScheduler.Models;

namespace UnifiedScheduler.Services;

public class JobAssigner
{
    private readonly IMqttClient _mqttClient;
    private readonly RedisService _redis;
    private readonly TimescaleDBService _timescale;
    private readonly JobQueueManager _queueManager;
    private readonly int _intervalMs;
    private CancellationTokenSource? _cts;

    public JobAssigner(
        IMqttClient mqttClient,
        RedisService redis,
        TimescaleDBService timescale,
        JobQueueManager queueManager,
        int intervalMs)
    {
        _mqttClient = mqttClient;
        _redis = redis;
        _timescale = timescale;
        _queueManager = queueManager;
        _intervalMs = intervalMs;
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Console.WriteLine($"[JobAssigner] Started (interval: {_intervalMs}ms)");

        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await AssignJobsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JobAssigner] Error: {ex.Message}");
                }

                await Task.Delay(_intervalMs, token);
            }
        }, token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        Console.WriteLine("[JobAssigner] Stopped");
    }

    private async Task AssignJobsAsync()
    {
        // Try to assign jobs for each job type
        await AssignJobTypeAsync("job_a", "A");
        await AssignJobTypeAsync("job_b", "B");
        await AssignJobTypeAsync("job_c", "C");
        await AssignJobTypeAsync("job_d", "D");
    }

    private async Task AssignJobTypeAsync(string jobType, string machineType)
    {
        // Get available machines for this type
        var availableMachines = await _redis.GetAvailableMachinesAsync(machineType);

        //console.WriteLine($"[JobAssigner] {jobType}: Found {availableMachines.Count} available machine(s) of type {machineType}");

        if (availableMachines.Count == 0)
            return; // No available machines

        // Try to assign jobs to all available machines
        foreach (var machine in availableMachines)
        {
            // Machine availability is already tracked in Redis via HeartbeatObserver
            // No need for additional checks here - Redis state is source of truth

            // Check if there's work in the queue
            var unitId = await _queueManager.DequeueJobAsync(jobType);
            //Console.WriteLine($"[JobAssigner] {jobType}: Dequeued unit {unitId ?? "null"}");
            if (unitId == null)
                break; // No more work in queue

            if (string.IsNullOrEmpty(unitId))
            {
                Console.WriteLine($"[JobAssigner] Invalid unit ID format: {unitId}");
                continue;
            }

            // Assign job to machine
            await AssignJobToMachineAsync(unitId, jobType, machine);
        }
    }

    private async Task AssignJobToMachineAsync(string unitId, string jobType, MachineState machine)
    {
        try
        {
            // Get unit info from Redis
            var unitState = await _redis.GetUnitStateAsync(unitId);
            if (unitState.Count == 0)
            {
                Console.WriteLine($"[JobAssigner] Unit {unitId} not found in Redis");
                return;
            }

            var orderIdStr = unitState.GetValueOrDefault("order_id", "");
            if (!int.TryParse(orderIdStr, out var orderId))
            {
                Console.WriteLine($"[JobAssigner] Invalid order_id for unit {unitId}");
                return;
            }

            var unitNumberStr = unitState.GetValueOrDefault("unit_number", "");
            if (!int.TryParse(unitNumberStr, out var unitNumber))
            {
                Console.WriteLine($"[JobAssigner] Invalid unit_number for unit {unitId}");
                return;
            }

            // Get order info from TimescaleDB (orders still in TimescaleDB)
            var order = await _timescale.GetOrderAsync(orderId);
            if (order == null)
            {
                Console.WriteLine($"[JobAssigner] Order {orderId} not found");
                return;
            }

            // Mark order as started if this is the first unit being assigned (job_a only)
            if (jobType == "job_a" && order.StartedAt == null)
            {
                await _timescale.MarkOrderStartedAsync(orderId);
            }

            // Update unit status to "running" in Redis (Redis is source of truth for unit state)
            await _redis.UpdateUnitJobAsync(unitId, jobType, "running", machine.MachineId);

            // Create work assignment payload
            var workAssignment = new
            {
                unit_id = unitId.ToString(),
                order_data = new
                {
                    order_id = order.Id.ToString(),
                    title = order.Title,
                    author = order.Author,
                    pages = order.Pages,
                    cover_type = order.CoverType,
                    paper_type = order.PaperType,
                    unit_number = unitNumber
                }
            };

            // Publish work assignment to machine
            var topic = $"machines/{machine.MachineId}/work";
            var payload = JsonConvert.SerializeObject(workAssignment);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(message);

            Console.WriteLine($"[JobAssigner] ✓ Assigned {jobType} for unit {unitId} (#{unitNumber}) to machine {machine.MachineId}");
            Console.WriteLine($"[JobAssigner]   Order: '{order.Title}' by {order.Author}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JobAssigner] Error assigning job: {ex.Message}");

            // Re-queue the job
            await _queueManager.EnqueueJobAsync(jobType, unitId.ToString());
        }
    }
}
