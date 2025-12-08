namespace UnifiedScheduler.Services;

public class JobQueueManager
{
    private readonly RedisService _redis;

    public JobQueueManager(RedisService redis)
    {
        _redis = redis;
    }

    public async Task EnqueueJobAsync(string jobType, string unitId, int priority = 1)
    {
        await _redis.EnqueueJobAsync(jobType, unitId, priority);

        var priorityLabel = priority switch
        {
            3 => "HIGH (re-queued)",
            2 => "MEDIUM (dependencies met)",
            1 => "LOW (new job)",
            _ => $"PRIORITY-{priority}"
        };

        Console.WriteLine($"[JobQueueManager] Enqueued unit {unitId} to {jobType} queue (priority: {priorityLabel})");
    }

    public async Task<string?> DequeueJobAsync(string jobType)
    {
        var unitId = await _redis.DequeueJobAsync(jobType);
        if (unitId != null)
        {
            Console.WriteLine($"[JobQueueManager] Dequeued unit {unitId} from {jobType} queue");
        }
        return unitId;
    }

    public async Task<long> GetQueueLengthAsync(string jobType)
    {
        return await _redis.GetQueueLengthAsync(jobType);
    }

    public async Task PrintQueueStatusAsync()
    {
        var queueA = await GetQueueLengthAsync("job_a");
        var queueB = await GetQueueLengthAsync("job_b");
        var queueC = await GetQueueLengthAsync("job_c");
        var queueD = await GetQueueLengthAsync("job_d");

        Console.WriteLine($"[JobQueueManager] Queue Status: " +
                        $"A={queueA}, B={queueB}, C={queueC}, D={queueD}");
    }
}
