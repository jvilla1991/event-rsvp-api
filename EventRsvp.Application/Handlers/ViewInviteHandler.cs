using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

/// <summary>
/// Public handler — called when the recipient opens their invite link.
/// Records the first-view timestamp and advances status to Opened.
/// </summary>
public class ViewInviteHandler
{
    private readonly IInviteRepository _inviteRepository;

    public ViewInviteHandler(IInviteRepository inviteRepository)
    {
        _inviteRepository = inviteRepository;
    }

    public async Task<InviteResponse> HandleAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidInviteException("Invite token is required.");

        var invite = await _inviteRepository.GetByTokenAsync(token, cancellationToken);
        if (invite == null)
            throw new InvalidInviteException($"Invite with token '{token}' not found.");

        // Only advance status on first view; never downgrade Accepted/Declined back to Opened.
        // Also preserve a pre-existing ViewedAt (e.g. an invite viewed before the Status
        // column existed will have ViewedAt set but Status = NotOpened).
        if (invite.Status == InviteStatus.NotOpened)
        {
            invite.Status = InviteStatus.Opened;
            invite.ViewedAt ??= DateTime.UtcNow;  // keep original timestamp if already stamped
            await _inviteRepository.UpdateAsync(invite, cancellationToken);
        }

        return new InviteResponse
        {
            Id = invite.Id,
            EventId = invite.EventId,
            Name = invite.Name,
            Token = invite.Token,
            Status = invite.Status.ToString(),
            ViewedAt = invite.ViewedAt,
            CreatedAt = invite.CreatedAt
        };
    }
}
