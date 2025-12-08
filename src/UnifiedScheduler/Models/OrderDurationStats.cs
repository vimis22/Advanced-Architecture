namespace UnifiedScheduler.Models;

public class OrderDurationStats
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double DurationSeconds { get; set; } // Processing time (started_at to completed_at)
    public double DurationMinutes { get; set; }
    public double WaitTimeSeconds { get; set; } // Wait time (created_at to started_at)
}
