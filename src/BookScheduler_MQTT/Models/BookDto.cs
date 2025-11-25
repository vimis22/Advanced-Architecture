// Models/BookDto.cs
using System;

namespace BookScheduler_MQTT.Models
{
    public class BookDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Copies { get; set; }
        public int Pages { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
