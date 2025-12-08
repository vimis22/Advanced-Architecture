namespace UnifiedScheduler.Models;

public class Unit
{
    public string Id { get; set; } = string.Empty; // Format: "{order_id}:{unit_number}"
    public int OrderId { get; set; }
    public int UnitNumber { get; set; }

    // Job A - Printing
    public string JobAStatus { get; set; } = "pending";
    public string? JobAMachine { get; set; }
    public DateTime? JobAStartedAt { get; set; }
    public DateTime? JobACompletedAt { get; set; }

    // Job B - Cover
    public string JobBStatus { get; set; } = "pending";
    public string? JobBMachine { get; set; }
    public DateTime? JobBStartedAt { get; set; }
    public DateTime? JobBCompletedAt { get; set; }

    // Job C - Binding
    public string JobCStatus { get; set; } = "waiting";
    public string? JobCMachine { get; set; }
    public DateTime? JobCStartedAt { get; set; }
    public DateTime? JobCCompletedAt { get; set; }

    // Job D - Packaging
    public string JobDStatus { get; set; } = "waiting";
    public string? JobDMachine { get; set; }
    public DateTime? JobDStartedAt { get; set; }
    public DateTime? JobDCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
