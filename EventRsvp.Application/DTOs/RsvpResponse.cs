namespace EventRsvp.Application.DTOs;

public class RsvpResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool WillAttend { get; set; }
    public DateTime CreatedAt { get; set; }
}

