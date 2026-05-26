using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetPollsByEventIdHandler
{
    private readonly IPollRepository _pollRepository;
    private readonly IEventRepository _eventRepository;

    public GetPollsByEventIdHandler(IPollRepository pollRepository, IEventRepository eventRepository)
    {
        _pollRepository = pollRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<PollResponse>> HandleAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new Domain.Exceptions.InvalidPollException($"Event with ID {eventId} not found.");
        }

        var polls = await _pollRepository.GetByEventIdAsync(eventId, cancellationToken);

        return polls.Select(p => new PollResponse
        {
            Id = p.Id,
            EventId = p.EventId,
            Question = p.Question,
            Options = p.Options,
            AllowMultiple = p.AllowMultiple,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        });
    }
}
