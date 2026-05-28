using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class CreateRsvpHandler
{
    private readonly IRsvpRepository _rsvpRepository;
    private readonly IEventRepository _eventRepository;

    public CreateRsvpHandler(IRsvpRepository rsvpRepository, IEventRepository eventRepository)
    {
        _rsvpRepository = rsvpRepository;
        _eventRepository = eventRepository;
    }

    public async Task<RsvpResponse> HandleAsync(int eventId, CreateRsvpRequest request, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new Domain.Exceptions.InvalidRsvpException($"Event with ID {eventId} not found.");
        }

        var rsvp = new Rsvp
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            WillAttend = request.WillAttend,
            ProposedTime = request.WillAttend ? null : request.ProposedTime,
            CreatedAt = DateTime.UtcNow
        };

        rsvp.Validate();

        var createdRsvp = await _rsvpRepository.AddAsync(rsvp, cancellationToken);

        return new RsvpResponse
        {
            Id = createdRsvp.Id,
            Name = createdRsvp.Name,
            WillAttend = createdRsvp.WillAttend,
            ProposedTime = createdRsvp.ProposedTime,
            CreatedAt = createdRsvp.CreatedAt
        };
    }
}

