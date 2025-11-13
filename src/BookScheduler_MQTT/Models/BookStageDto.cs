using System;

public class BookStageDto
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string Stage { get; set; }
    public string Status { get; set; }
    public Guid? AssignedMachine { get; set; }
    public int Progress { get; set; }
    public DateTime UpdatedAt { get; set; }
}
