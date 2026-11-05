using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class Rsvp
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the person is attending: Yes, No, or Maybe.
    /// </summary>
    public RsvpStatus Status { get; set; } = RsvpStatus.Yes;

    /// <summary>
    /// Optional time proposed by the attendee when they cannot make the original time
    /// </summary>
    public DateTime? ProposedTime { get; set; }

    /// <summary>
    /// Optional contact email supplied by the attendee
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Optional number of guests in the attendee's party (including themselves)
    /// </summary>
    public int? GuestCount { get; set; }

    /// <summary>
    /// Optional meal selection for events that offer one (e.g. weddings)
    /// </summary>
    public string? MealChoice { get; set; }

    /// <summary>
    /// Optional free-text note from the attendee to the host
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Names of the dish(es) this attendee is bringing to a "bring a dish" event.
    /// Only populated for "Yes" responses on events where <see cref="Event.IsBringADish"/>
    /// is true; empty for every other RSVP.
    /// </summary>
    public List<string> Dishes { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (EventId <= 0)
        {
            throw new InvalidRsvpException("Event ID is required and must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidRsvpException("Name is required and cannot be empty.");
        }
    }
}
