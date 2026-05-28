namespace EventRsvp.Application.DTOs;

public class EventResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? EventDateTime { get; set; }
    public string? Address { get; set; }
    public bool AllowTimeProposal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
