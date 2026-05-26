namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for creating a new RSVP
/// </summary>
/// <example>
/// {
///   "name": "John Doe",
///   "willAttend": true
/// }
/// </example>
public class CreateRsvpRequest
{
    /// <summary>
    /// The name of the person RSVPing (required, max 200 characters)
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the person will attend the event (default: true)
    /// </summary>
    /// <example>true</example>
    public bool WillAttend { get; set; } = true;
}

