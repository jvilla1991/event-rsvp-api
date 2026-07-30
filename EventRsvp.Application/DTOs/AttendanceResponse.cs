namespace EventRsvp.Application.DTOs;

public class AttendanceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// NotOpened, Opened, Accepted, Declined, or Maybe
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The attendee's response: "Yes", "No", "Maybe", or null if not yet responded.
    /// </summary>
    public string? Response { get; set; }

    public DateTime? ProposedTime { get; set; }

    /// <summary>
    /// Optional contact email supplied by the attendee.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Optional number of guests in the attendee's party, including themselves.
    /// </summary>
    public int? GuestCount { get; set; }

    /// <summary>
    /// Optional meal selection for events that offer one.
    /// </summary>
    public string? MealChoice { get; set; }

    /// <summary>
    /// Optional free-text note from the attendee to the host.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// "invite" for tracked invitees, "rsvp" for walk-in responses
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
