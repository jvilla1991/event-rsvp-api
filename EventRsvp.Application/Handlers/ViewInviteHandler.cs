using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

/// <summary>
/// Public handler — called when the recipient opens their invite link.
/// Records the first-view timestamp and returns invite details so the
/// frontend knows which event page to render.
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

        // Only record the first view
        if (!invite.ViewedAt.HasValue)
        {
            invite.ViewedAt = DateTime.UtcNow;
            await _inviteRepository.UpdateAsync(invite, cancellationToken);
        }

        return new InviteResponse
        {
            Id = invite.Id,
            EventId = invite.EventId,
            Name = invite.Name,
            Token = invite.Token,
            ViewedAt = invite.ViewedAt,
            CreatedAt = invite.CreatedAt
        };
    }
}
