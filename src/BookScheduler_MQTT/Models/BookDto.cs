// Models/BookDto.cs
using System;

public class BookDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public int Copies { get; set; }
    public int Pages { get; set; }
    public DateTime CreatedAt { get; set; }
}