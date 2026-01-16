namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for creating a new RSVP
/// </summary>
public class CreateRsvpRequest
{
    /// <summary>
    /// The name of the person RSVPing (required, max 200 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the person will attend the event (default: true)
    /// </summary>
    public bool WillAttend { get; set; } = true;
}

