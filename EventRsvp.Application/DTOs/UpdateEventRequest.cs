namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for updating an existing event
/// </summary>
/// <example>
/// {
///   "title": "Updated Summer Tech Conference 2024",
///   "description": "Updated description with new speakers and agenda",
///   "eventDateTime": "2024-07-20T10:00:00Z",
///   "address": "456 New Venue, San Francisco, CA 94103"
/// }
/// </example>
public class UpdateEventRequest
{
    /// <summary>
    /// The title of the event (required, max 200 characters)
    /// </summary>
    /// <example>Updated Summer Tech Conference 2024</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the event (max 1000 characters)
    /// </summary>
    /// <example>Updated description with new speakers and agenda</example>
    public string? Description { get; set; }

    /// <summary>
    /// Optional date and time of the event
    /// </summary>
    /// <example>2024-07-20T10:00:00Z</example>
    public DateTime? EventDateTime { get; set; }

    /// <summary>
    /// Optional address where the event will take place (max 500 characters)
    /// </summary>
    /// <example>456 New Venue, San Francisco, CA 94103</example>
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
