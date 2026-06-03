using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class Invite
{
    public int Id { get; set; }
    public int EventId { get; set; }

    /// <summary>
    /// Optional name identifying who this invite was sent to (e.g. "Bob").
    /// Empty string when created anonymously.
    /// </summary>
    public string Name { get; set; } = string.Empty;  // stored as empty when not provided

    /// <summary>
    /// Unique token embedded in the shareable link
    /// </summary>
    public string Token { get; set; } = string.Empty;

    public InviteStatus Status { get; set; } = InviteStatus.NotOpened;

    /// <summary>
    /// Set when the recipient first opens the invite link; null if not yet viewed
    /// </summary>
    public DateTime? ViewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (EventId <= 0)
            throw new InvalidInviteException("Event ID is required and must be greater than zero.");

        // Name is optional — no validation required

        if (string.IsNullOrWhiteSpace(Token))
            throw new InvalidInviteException("Invite token is required.");
    }
}
