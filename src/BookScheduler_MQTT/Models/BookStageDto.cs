// Models/BookStageDto.cs
using System;

namespace BookScheduler_MQTT.Models
{
    public class BookStageDto
    {
        public Guid Id { get; set; }
        public Guid BookId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public string Status { get; set; } = "queued";
        public Guid? AssignedMachine { get; set; }
        public int Progress { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
