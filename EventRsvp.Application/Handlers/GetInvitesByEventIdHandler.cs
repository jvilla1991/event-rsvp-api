using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetInvitesByEventIdHandler
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IEventRepository _eventRepository;

    public GetInvitesByEventIdHandler(IInviteRepository inviteRepository, IEventRepository eventRepository)
    {
        _inviteRepository = inviteRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<InviteResponse>> HandleAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
            throw new InvalidInviteException($"Event with ID {eventId} not found.");

        var invites = await _inviteRepository.GetByEventIdAsync(eventId, cancellationToken);

        return invites.Select(i => new InviteResponse
        {
            Id = i.Id,
            EventId = i.EventId,
            Name = i.Name,
            Token = i.Token,
            Status = i.Status.ToString(),
            ViewedAt = i.ViewedAt,
            CreatedAt = i.CreatedAt
        });
    }
}
