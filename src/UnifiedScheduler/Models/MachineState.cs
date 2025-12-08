namespace UnifiedScheduler.Models;

public class MachineState
{
    public string MachineId { get; set; } = string.Empty;
    public string MachineType { get; set; } = string.Empty; // A, B, C, D
    public string Status { get; set; } = "off"; // idle, running, off
    public string? CurrentUnitId { get; set; }
    public int? Progress { get; set; }
    public DateTime LastHeartbeat { get; set; }
}
