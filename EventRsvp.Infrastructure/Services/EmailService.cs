using EventRsvp.Application.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EventRsvp.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendTimeProposalNotificationAsync(
        string attendeeName,
        string eventTitle,
        DateTime proposedTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
            string.IsNullOrWhiteSpace(_settings.NotificationAddress))
        {
            _logger.LogWarning("Email not configured — skipping time proposal notification for '{EventTitle}'.", eventTitle);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(string.Empty, _settings.NotificationAddress));
            message.Subject = $"New Time Proposal for \"{eventTitle}\"";

            var localTime = proposedTime.ToLocalTime();
            var body = new BodyBuilder
            {
                TextBody = $"""
                    {attendeeName} can't make it to "{eventTitle}" and has proposed a new time.

                    Proposed Time: {localTime:dddd, MMMM d, yyyy} at {localTime:h:mm tt}

                    Log in to the admin dashboard to view all RSVPs for this event.
                    """
            };

            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Time proposal notification sent to {Recipient} for event '{EventTitle}'.",
                _settings.NotificationAddress, eventTitle);
        }
        catch (Exception ex)
        {
            // Email failure must never fail an RSVP submission
            _logger.LogError(ex,
                "Failed to send time proposal notification for event '{EventTitle}'. RSVP was still saved.",
                eventTitle);
        }
    }
}
