namespace UnifiedScheduler.Models;

public class RecoverySummary
{
    public int TotalRecoveries { get; set; }
    public double AvgRecoveryMs { get; set; }
    public int MinRecoveryMs { get; set; }
    public int MaxRecoveryMs { get; set; }
    public double MedianRecoveryMs { get; set; }
}
