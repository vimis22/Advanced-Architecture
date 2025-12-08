using Npgsql;
using Dapper;
using UnifiedScheduler.Models;

namespace UnifiedScheduler.Services;

public class TimescaleDBService
{
    private readonly string _connectionString;

    public TimescaleDBService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

    // ============ Orders ============

    public async Task<int> CreateOrderAsync(CreateOrderRequest request)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sql = @"
            INSERT INTO orders (title, author, pages, cover_type, paper_type, quantity, status, created_at)
            VALUES (@Title, @Author, @Pages, @CoverType, @PaperType, @Quantity, 'pending', NOW())
            RETURNING id";

        // Create explicit parameter object instead of using request directly
        // This avoids issues with JsonProperty attributes interfering with Dapper
        // Use QueryFirstAsync instead of ExecuteScalarAsync to avoid IConvertible issues
        var orderId = await conn.QueryFirstAsync<int>(sql, new
        {
            Title = request.Title,
            Author = request.Author,
            Pages = request.Pages,
            CoverType = request.CoverType,
            PaperType = request.PaperType,
            Quantity = request.Quantity
        });
        return orderId;
    }

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        using var conn = GetConnection();
        var sql = "SELECT * FROM orders WHERE id = @OrderId";
        return await conn.QueryFirstOrDefaultAsync<Order>(sql, new { OrderId = orderId });
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        using var conn = GetConnection();
        var sql = "SELECT * FROM orders ORDER BY created_at DESC";
        var orders = await conn.QueryAsync<Order>(sql);
        return orders.ToList();
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status, DateTime? completedAt = null)
    {
        using var conn = GetConnection();
        var sql = @"
            UPDATE orders
            SET status = @Status, completed_at = @CompletedAt
            WHERE id = @OrderId";

        await conn.ExecuteAsync(sql, new { OrderId = orderId, Status = status, CompletedAt = completedAt });
    }

    public async Task MarkOrderStartedAsync(int orderId)
    {
        using var conn = GetConnection();
        var sql = @"
            UPDATE orders
            SET started_at = NOW(), status = 'processing'
            WHERE id = @OrderId AND started_at IS NULL";

        await conn.ExecuteAsync(sql, new { OrderId = orderId });
    }

    public async Task MarkOrderCompletedAsync(int orderId)
    {
        using var conn = GetConnection();
        var sql = @"
            UPDATE orders
            SET completed_at = NOW(), status = 'completed'
            WHERE id = @OrderId AND completed_at IS NULL";

        await conn.ExecuteAsync(sql, new { OrderId = orderId });
    }

    // ============ Requeue Events ============

    public async Task LogRequeueEventAsync(
        string unitId,
        int orderId,
        string jobType,
        string machineId,
        string machineType,
        string reason,
        DateTime failureDetectedAt,
        DateTime requeuedAt)
    {
        using var conn = GetConnection();
        var recoveryDurationMs = (int)(requeuedAt - failureDetectedAt).TotalMilliseconds;

        var sql = @"
            INSERT INTO requeue_events (
                unit_id,
                order_id,
                job_type,
                machine_id,
                machine_type,
                reason,
                failure_detected_at,
                unit_requeued_at,
                recovery_duration_ms,
                timestamp
            )
            VALUES (
                @UnitId,
                @OrderId,
                @JobType,
                @MachineId,
                @MachineType,
                @Reason,
                @FailureDetectedAt,
                @RequeuedAt,
                @RecoveryDurationMs,
                @RequeuedAt
            )";

        await conn.ExecuteAsync(sql, new
        {
            UnitId = unitId,
            OrderId = orderId,
            JobType = jobType,
            MachineId = machineId,
            MachineType = machineType,
            Reason = reason,
            FailureDetectedAt = failureDetectedAt,
            RequeuedAt = requeuedAt,
            RecoveryDurationMs = recoveryDurationMs
        });
    }

    // ============ Statistics Queries ============

    public async Task<List<OrderDurationStats>> GetOrderDurationStatisticsAsync()
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT
                id as Id,
                title as Title,
                quantity as Quantity,
                created_at as CreatedAt,
                started_at as StartedAt,
                completed_at as CompletedAt,
                EXTRACT(EPOCH FROM (completed_at - started_at)) as DurationSeconds,
                EXTRACT(EPOCH FROM (completed_at - started_at)) / 60 as DurationMinutes,
                EXTRACT(EPOCH FROM (started_at - created_at)) as WaitTimeSeconds
            FROM orders
            WHERE status = 'completed' AND completed_at IS NOT NULL AND started_at IS NOT NULL
            ORDER BY created_at DESC";

        var results = await conn.QueryAsync<OrderDurationStats>(sql);
        return results.ToList();
    }

    public async Task<List<RecoveryStats>> GetRecoveryStatisticsAsync()
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT
                machine_id as MachineId,
                machine_type as MachineType,
                unit_id as UnitId,
                recovery_duration_ms as RecoveryDurationMs,
                failure_detected_at as FailureDetectedAt,
                unit_requeued_at as UnitRequeuedAt,
                timestamp as Timestamp
            FROM requeue_events
            ORDER BY timestamp DESC";

        var results = await conn.QueryAsync<RecoveryStats>(sql);
        return results.ToList();
    }

    public async Task<RecoverySummary> GetRecoverySummaryAsync()
    {
        using var conn = GetConnection();
        var sql = @"
            SELECT
                COUNT(*) as TotalRecoveries,
                AVG(recovery_duration_ms) as AvgRecoveryMs,
                MIN(recovery_duration_ms) as MinRecoveryMs,
                MAX(recovery_duration_ms) as MaxRecoveryMs,
                PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY recovery_duration_ms) as MedianRecoveryMs
            FROM requeue_events";

        return await conn.QueryFirstOrDefaultAsync<RecoverySummary>(sql) ?? new RecoverySummary();
    }
}
