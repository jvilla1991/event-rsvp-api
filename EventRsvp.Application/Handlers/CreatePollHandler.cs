using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class CreatePollHandler
{
    private readonly IPollRepository _pollRepository;
    private readonly IEventRepository _eventRepository;

    public CreatePollHandler(IPollRepository pollRepository, IEventRepository eventRepository)
    {
        _pollRepository = pollRepository;
        _eventRepository = eventRepository;
    }

    public async Task<PollResponse> HandleAsync(int eventId, CreatePollRequest request, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new Domain.Exceptions.InvalidPollException($"Event with ID {eventId} not found.");
        }

        var poll = new Poll
        {
            EventId = eventId,
            Question = request.Question.Trim(),
            Options = request.Options.Select(opt => opt.Trim()).ToList(),
            AllowMultiple = request.AllowMultiple,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        poll.Validate();

        var createdPoll = await _pollRepository.AddAsync(poll, cancellationToken);

        return new PollResponse
        {
            Id = createdPoll.Id,
            EventId = createdPoll.EventId,
            Question = createdPoll.Question,
            Options = createdPoll.Options,
            AllowMultiple = createdPoll.AllowMultiple,
            CreatedAt = createdPoll.CreatedAt,
            UpdatedAt = createdPoll.UpdatedAt
        };
    }
}
