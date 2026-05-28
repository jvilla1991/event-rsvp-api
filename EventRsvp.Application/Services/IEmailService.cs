namespace EventRsvp.Application.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends an email notification to the configured address when an attendee
    /// proposes an alternative time instead of attending.
    /// Implementations must not throw — email failure should never fail an RSVP.
    /// </summary>
    Task SendTimeProposalNotificationAsync(
        string attendeeName,
        string eventTitle,
        DateTime proposedTime,
        CancellationToken cancellationToken = default);
}
