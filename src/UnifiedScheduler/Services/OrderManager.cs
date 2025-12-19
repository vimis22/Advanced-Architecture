using UnifiedScheduler.Models;

namespace UnifiedScheduler.Services;

public class OrderManager
{
    private readonly RedisService _redis;
    private readonly TimescaleDBService _timescale;
    private readonly JobQueueManager _queueManager;

    public OrderManager(
        RedisService redis,
        TimescaleDBService timescale,
        JobQueueManager queueManager)
    {
        _redis = redis;
        _timescale = timescale;
        _queueManager = queueManager;
    }

    public async Task<int> CreateOrderAsync(CreateOrderRequest request)
    {
        Console.WriteLine($"[OrderManager] Creating order: '{request.Title}' by {request.Author}");
        Console.WriteLine($"[OrderManager]   Pages: {request.Pages}, Cover: {request.CoverType}, " +
                        $"Paper: {request.PaperType}, Quantity: {request.Quantity}");

        // Create order in TimescaleDB (only stores order info, not units)
        var orderId = await _timescale.CreateOrderAsync(request);

        // Create units in Redis (source of truth for unit state)
        // Unit ID format: {order_id}:{unit_number}
        var unitIds = new List<string>();
        for (int i = 1; i <= request.Quantity; i++)
        {
            var unitId = $"{orderId}:{i}";
            unitIds.Add(unitId);

            // Initialize unit state in Redis
            await _redis.InitializeUnitAsync(unitId, orderId, i);
        }

        Console.WriteLine($"[OrderManager] Created order {orderId} with {request.Quantity} units in Redis");

        // Queue all units for job_a and job_b (parallel jobs)
        foreach (var unitId in unitIds)
        {
            await _queueManager.EnqueueJobAsync("job_a", unitId);
            await _queueManager.EnqueueJobAsync("job_b", unitId);
        }

        Console.WriteLine($"[OrderManager] Queued {request.Quantity} units to job_a and job_b queues");

        // Update order status to processing
        await _timescale.UpdateOrderStatusAsync(orderId, "processing");

        return orderId;
    }

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        return await _timescale.GetOrderAsync(orderId);
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetOrderUnitsAsync(int orderId)
    {
        // Get all unit IDs for this order from Redis
        var unitIds = await _redis.GetOrderUnitIdsAsync(orderId);
        var units = new Dictionary<string, Dictionary<string, string>>();

        foreach (var unitId in unitIds)
        {
            var state = await _redis.GetUnitStateAsync(unitId);
            units[unitId] = state;
        }

        return units;
    }

    public async Task PrintOrderStatusAsync(int orderId)
    {
        var order = await GetOrderAsync(orderId);
        if (order == null)
        {
            Console.WriteLine($"[OrderManager] Order {orderId} not found");
            return;
        }

        var units = await GetOrderUnitsAsync(orderId);
        var completedUnits = units.Count(u => u.Value.GetValueOrDefault("job_d_status") == "completed");

        Console.WriteLine($"\n[OrderManager] === Order Status: {order.Title} ===");
        Console.WriteLine($"  Order ID: {orderId}");
        Console.WriteLine($"  Status: {order.Status}");
        Console.WriteLine($"  Completed Units: {completedUnits}/{order.Quantity}");

        var inProgressA = units.Count(u => u.Value.GetValueOrDefault("job_a_status") == "running");
        var inProgressB = units.Count(u => u.Value.GetValueOrDefault("job_b_status") == "running");
        var inProgressC = units.Count(u => u.Value.GetValueOrDefault("job_c_status") == "running");
        var inProgressD = units.Count(u => u.Value.GetValueOrDefault("job_d_status") == "running");

        Console.WriteLine($"  In Progress: A={inProgressA}, B={inProgressB}, C={inProgressC}, D={inProgressD}");
        Console.WriteLine();
    }
}
