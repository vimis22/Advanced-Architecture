namespace UnifiedScheduler.Models;

public class Order
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Pages { get; set; }
    public string CoverType { get; set; } = string.Empty;
    public string PaperType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; } // When first unit was assigned to a machine
    public DateTime? CompletedAt { get; set; }
}

public class CreateOrderRequest
{
    [Newtonsoft.Json.JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [Newtonsoft.Json.JsonProperty("author")]
    public string Author { get; set; } = string.Empty;

    [Newtonsoft.Json.JsonProperty("pages")]
    public int Pages { get; set; }

    [Newtonsoft.Json.JsonProperty("cover_type")]
    public string CoverType { get; set; } = string.Empty;

    [Newtonsoft.Json.JsonProperty("paper_type")]
    public string PaperType { get; set; } = string.Empty;

    [Newtonsoft.Json.JsonProperty("quantity")]
    public int Quantity { get; set; }
}
