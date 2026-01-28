namespace EventRsvp.Application.DTOs;

/// <summary>
/// Response DTO for poll data
/// </summary>
public class PollResponse
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public bool AllowMultiple { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
