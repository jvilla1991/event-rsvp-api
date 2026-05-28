using EventRsvp.Application.DTOs;
using EventRsvp.Application.Services;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class CreateRsvpHandler
{
    private readonly IRsvpRepository _rsvpRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;

    public CreateRsvpHandler(
        IRsvpRepository rsvpRepository,
        IEventRepository eventRepository,
        IEmailService emailService)
    {
        _rsvpRepository = rsvpRepository;
        _eventRepository = eventRepository;
        _emailService = emailService;
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

        // Send notification if the attendee proposed an alternative time
        if (createdRsvp.ProposedTime.HasValue)
        {
            await _emailService.SendTimeProposalNotificationAsync(
                createdRsvp.Name,
                eventEntity.Title,
                createdRsvp.ProposedTime.Value,
                cancellationToken);
        }

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
