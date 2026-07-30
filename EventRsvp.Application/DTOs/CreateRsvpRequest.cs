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

    /// <summary>
    /// Optional contact email of the person RSVPing (max 320 characters).
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string? Email { get; set; }

    /// <summary>
    /// Optional number of guests in the party, including the person RSVPing (1-4).
    /// </summary>
    /// <example>2</example>
    public int? GuestCount { get; set; }

    /// <summary>
    /// Optional meal selection for events that offer one (max 100 characters).
    /// </summary>
    /// <example>Vegetarian</example>
    public string? MealChoice { get; set; }

    /// <summary>
    /// Optional free-text note to the host (max 1000 characters).
    /// </summary>
    public string? Note { get; set; }
}

