namespace UnifiedScheduler.Models;

public class RecoveryStats
{
    public string MachineId { get; set; } = string.Empty;
    public string MachineType { get; set; } = string.Empty;
    public string UnitId { get; set; } = string.Empty;
    public int RecoveryDurationMs { get; set; }
    public DateTime FailureDetectedAt { get; set; }
    public DateTime UnitRequeuedAt { get; set; }
    public DateTime Timestamp { get; set; }
}
