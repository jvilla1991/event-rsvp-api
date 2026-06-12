using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

/// <summary>
/// Removes a person from an event's unified attendance list. Because the list merges
/// two backing records — tracked invites and walk-in RSVPs — the source decides which
/// record(s) to delete. Deleting an invited person also removes their matching RSVP so
/// they don't reappear as a walk-in row on the next load.
/// </summary>
public class DeleteAttendanceHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IInviteRepository _inviteRepository;
    private readonly IRsvpRepository _rsvpRepository;

    public DeleteAttendanceHandler(
        IEventRepository eventRepository,
        IInviteRepository inviteRepository,
        IRsvpRepository rsvpRepository)
    {
        _eventRepository = eventRepository;
        _inviteRepository = inviteRepository;
        _rsvpRepository = rsvpRepository;
    }

    public async Task<bool> HandleAsync(int eventId, string source, int id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
            throw new InvalidRsvpException($"Event with ID {eventId} not found.");

        return source?.ToLowerInvariant() switch
        {
            "invite" => await DeleteInviteRowAsync(eventId, id, cancellationToken),
            "rsvp" => await DeleteRsvpRowAsync(eventId, id, cancellationToken),
            _ => throw new InvalidRsvpException($"Unknown attendance source '{source}'. Expected 'invite' or 'rsvp'.")
        };
    }

    private async Task<bool> DeleteInviteRowAsync(int eventId, int inviteId, CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByIdAsync(inviteId, cancellationToken);
        if (invite == null)
            return false;

        if (invite.EventId != eventId)
            throw new InvalidRsvpException($"Invite with ID {inviteId} does not belong to event {eventId}.");

        // Also remove any RSVP this invitee submitted so they don't resurface as a walk-in row.
        if (!string.IsNullOrWhiteSpace(invite.Name))
        {
            var matchingRsvp = await _rsvpRepository.GetByEventIdAndNameAsync(eventId, invite.Name, cancellationToken);
            if (matchingRsvp != null)
                await _rsvpRepository.DeleteAsync(matchingRsvp.Id, cancellationToken);
        }

        return await _inviteRepository.DeleteAsync(inviteId, cancellationToken);
    }

    private async Task<bool> DeleteRsvpRowAsync(int eventId, int rsvpId, CancellationToken cancellationToken)
    {
        var rsvp = await _rsvpRepository.GetByIdAsync(rsvpId, cancellationToken);
        if (rsvp == null)
            return false;

        if (rsvp.EventId != eventId)
            throw new InvalidRsvpException($"RSVP with ID {rsvpId} does not belong to event {eventId}.");

        return await _rsvpRepository.DeleteAsync(rsvpId, cancellationToken);
    }
}
