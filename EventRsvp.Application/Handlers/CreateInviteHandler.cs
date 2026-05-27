using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class CreateInviteHandler
{
    private readonly IInviteRepository _inviteRepository;
    private readonly IEventRepository _eventRepository;

    public CreateInviteHandler(IInviteRepository inviteRepository, IEventRepository eventRepository)
    {
        _inviteRepository = inviteRepository;
        _eventRepository = eventRepository;
    }

    public async Task<InviteResponse> HandleAsync(int eventId, CreateInviteRequest request, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
            throw new InvalidInviteException($"Event with ID {eventId} not found.");

        var invite = new Invite
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            Token = Guid.NewGuid().ToString("N"), // compact lowercase hex token
            CreatedAt = DateTime.UtcNow
        };

        invite.Validate();

        var created = await _inviteRepository.AddAsync(invite, cancellationToken);

        return MapToResponse(created);
    }

    private static InviteResponse MapToResponse(Invite invite) => new()
    {
        Id = invite.Id,
        EventId = invite.EventId,
        Name = invite.Name,
        Token = invite.Token,
        ViewedAt = invite.ViewedAt,
        CreatedAt = invite.CreatedAt
    };
}
