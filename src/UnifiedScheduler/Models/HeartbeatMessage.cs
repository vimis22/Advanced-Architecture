using Newtonsoft.Json;

namespace UnifiedScheduler.Models;

public class HeartbeatMessage
{
    [JsonProperty("machine_id")]
    public string MachineId { get; set; } = string.Empty;

    [JsonProperty("machine_type")]
    public string MachineType { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty; // idle, running, off

    [JsonProperty("current_unit_id")]
    public string? CurrentUnitId { get; set; }

    [JsonProperty("progress")]
    public int? Progress { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }
}
