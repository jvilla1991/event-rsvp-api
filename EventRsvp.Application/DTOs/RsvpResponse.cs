namespace EventRsvp.Application.DTOs;

public class RsvpResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The attendance response: "Yes", "No", or "Maybe".
    /// </summary>
    public string Status { get; set; } = "Yes";

    public DateTime? ProposedTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

