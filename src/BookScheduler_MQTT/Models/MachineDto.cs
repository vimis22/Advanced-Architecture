// Models/MachineDto.cs
using System;

namespace BookScheduler_MQTT.Models
{
    public class MachineDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? PagesPerMin { get; set; }
        public string Status { get; set; } = "off";
        public DateTime? LastSeen { get; set; }
        public string Metadata { get; set; } = "{}";
    }
}
