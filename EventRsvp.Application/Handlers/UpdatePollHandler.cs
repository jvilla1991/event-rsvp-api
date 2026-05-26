using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class UpdatePollHandler
{
    private readonly IPollRepository _pollRepository;
    private readonly IEventRepository _eventRepository;

    public UpdatePollHandler(IPollRepository pollRepository, IEventRepository eventRepository)
    {
        _pollRepository = pollRepository;
        _eventRepository = eventRepository;
    }

    public async Task<PollResponse?> HandleAsync(int eventId, int pollId, UpdatePollRequest request, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new Domain.Exceptions.InvalidPollException($"Event with ID {eventId} not found.");
        }

        var poll = await _pollRepository.GetByIdAsync(pollId, cancellationToken);
        if (poll == null)
        {
            return null;
        }

        // Validate that poll belongs to the event
        if (poll.EventId != eventId)
        {
            throw new Domain.Exceptions.InvalidPollException($"Poll with ID {pollId} does not belong to event with ID {eventId}.");
        }

        poll.Question = request.Question.Trim();
        poll.Options = request.Options.Select(opt => opt.Trim()).ToList();
        poll.AllowMultiple = request.AllowMultiple;
        poll.UpdatedAt = DateTime.UtcNow;

        poll.Validate();

        var updatedPoll = await _pollRepository.UpdateAsync(poll, cancellationToken);

        return new PollResponse
        {
            Id = updatedPoll.Id,
            EventId = updatedPoll.EventId,
            Question = updatedPoll.Question,
            Options = updatedPoll.Options,
            AllowMultiple = updatedPoll.AllowMultiple,
            CreatedAt = updatedPoll.CreatedAt,
            UpdatedAt = updatedPoll.UpdatedAt
        };
    }
}
