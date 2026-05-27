using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class Invite
{
    public int Id { get; set; }
    public int EventId { get; set; }

    /// <summary>
    /// The name of the person this invite was sent to (e.g. "Bob")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique token embedded in the shareable link
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Set when the recipient first opens the invite link; null if not yet viewed
    /// </summary>
    public DateTime? ViewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (EventId <= 0)
            throw new InvalidInviteException("Event ID is required and must be greater than zero.");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidInviteException("Invite name is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(Token))
            throw new InvalidInviteException("Invite token is required.");
    }
}
