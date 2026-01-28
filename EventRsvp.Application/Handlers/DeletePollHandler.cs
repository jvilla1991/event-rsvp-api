using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class DeletePollHandler
{
    private readonly IPollRepository _pollRepository;
    private readonly IPollVoteRepository _pollVoteRepository;
    private readonly IEventRepository _eventRepository;

    public DeletePollHandler(
        IPollRepository pollRepository,
        IPollVoteRepository pollVoteRepository,
        IEventRepository eventRepository)
    {
        _pollRepository = pollRepository;
        _pollVoteRepository = pollVoteRepository;
        _eventRepository = eventRepository;
    }

    public async Task<bool> HandleAsync(int eventId, int pollId, CancellationToken cancellationToken = default)
    {
        // Validate that event exists
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new Domain.Exceptions.InvalidPollException($"Event with ID {eventId} not found.");
        }

        var poll = await _pollRepository.GetByIdAsync(pollId, cancellationToken);
        if (poll == null)
        {
            return false;
        }

        if (poll.EventId != eventId)
        {
            throw new Domain.Exceptions.InvalidPollException($"Poll with ID {pollId} does not belong to event with ID {eventId}.");
        }

        // Delete all votes for this poll
        await _pollVoteRepository.DeleteByPollIdAsync(pollId, cancellationToken);

        return await _pollRepository.DeleteAsync(pollId, cancellationToken);
    }
}
