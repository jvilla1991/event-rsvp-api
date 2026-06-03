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

    /// <summary>
    /// Optional alternative time proposed by the person when they cannot attend.
    /// Only meaningful when WillAttend is false.
    /// </summary>
    public DateTime? ProposedTime { get; set; }

    /// <summary>
    /// Optional invite token from the shareable link. When provided, the matching
    /// invite's status is updated to Accepted or Declined automatically.
    /// </summary>
    public string? InviteToken { get; set; }
}

