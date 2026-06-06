namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for creating a new event
/// </summary>
/// <example>
/// {
///   "title": "Summer Tech Conference 2024",
///   "description": "Annual technology conference featuring talks on AI, cloud computing, and software engineering",
///   "eventDateTime": "2024-07-15T10:00:00Z",
///   "address": "123 Convention Center, San Francisco, CA 94102"
/// }
/// </example>
public class CreateEventRequest
{
    /// <summary>
    /// The title of the event (required, max 200 characters)
    /// </summary>
    /// <example>Summer Tech Conference 2024</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the event (max 1000 characters)
    /// </summary>
    /// <example>Annual technology conference featuring talks on AI, cloud computing, and software engineering</example>
    public string? Description { get; set; }

    /// <summary>
    /// Optional date and time of the event
    /// </summary>
    /// <example>2024-07-15T10:00:00Z</example>
    public DateTime? EventDateTime { get; set; }

    /// <summary>
    /// Optional address where the event will take place (max 500 characters)
    /// </summary>
    /// <example>123 Convention Center, San Francisco, CA 94102</example>
    public string? Address { get; set; }

    /// <summary>
    /// When true, attendees who select "No" are prompted to propose an alternative time.
    /// Only settable by admins. Defaults to false.
    /// </summary>
    /// <example>false</example>
    public bool AllowTimeProposal { get; set; }

    /// <summary>
    /// When true, public (non-admin) visitors may create polls on this event.
    /// Only settable by admins. Defaults to false.
    /// </summary>
    /// <example>false</example>
    public bool AllowGuestPolls { get; set; }
}
