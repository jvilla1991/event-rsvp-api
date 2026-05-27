using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class DeleteInviteHandler
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IEventRepository _eventRepository;

    public DeleteInviteHandler(IInviteRepository inviteRepository, IEventRepository eventRepository)
    {
        _inviteRepository = inviteRepository;
        _eventRepository = eventRepository;
    }

    public async Task<bool> HandleAsync(int eventId, int inviteId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
            throw new InvalidInviteException($"Event with ID {eventId} not found.");

        var invite = await _inviteRepository.GetByIdAsync(inviteId, cancellationToken);
        if (invite == null)
            return false;

        if (invite.EventId != eventId)
            throw new InvalidInviteException($"Invite with ID {inviteId} does not belong to event {eventId}.");

        return await _inviteRepository.DeleteAsync(inviteId, cancellationToken);
    }
}
