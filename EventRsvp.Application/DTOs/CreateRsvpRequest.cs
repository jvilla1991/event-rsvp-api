namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for creating a new RSVP
/// </summary>
/// <example>
/// {
///   "name": "John Doe",
///   "status": "Yes"
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
    /// The attendance response: "Yes", "No", or "Maybe" (case-insensitive, default: "Yes").
    /// </summary>
    /// <example>Yes</example>
    public string Status { get; set; } = "Yes";

    /// <summary>
    /// Optional alternative time proposed by the person.
    /// Only meaningful when Status is "No" or "Maybe".
    /// </summary>
    public DateTime? ProposedTime { get; set; }

    /// <summary>
    /// Optional invite token from the shareable link. When provided, the matching
    /// invite's status is updated to Accepted or Declined automatically.
    /// </summary>
    public string? InviteToken { get; set; }
}

