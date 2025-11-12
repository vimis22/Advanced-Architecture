using System;

public class MachineDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public int? PagesPerMin { get; set; }
    public bool IsUp { get; set; }
    public bool IsBusy { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? Metadata { get; set; } // JSON string or null
}