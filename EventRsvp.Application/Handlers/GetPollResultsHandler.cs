using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetPollResultsHandler
{
    private readonly IPollRepository _pollRepository;
    private readonly IPollVoteRepository _pollVoteRepository;
    private readonly IEventRepository _eventRepository;

    public GetPollResultsHandler(
        IPollRepository pollRepository,
        IPollVoteRepository pollVoteRepository,
        IEventRepository eventRepository)
    {
        _pollRepository = pollRepository;
        _pollVoteRepository = pollVoteRepository;
        _eventRepository = eventRepository;
    }

    public async Task<PollResultsResponse> HandleAsync(int eventId, int pollId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new Domain.Exceptions.InvalidPollException($"Event with ID {eventId} not found.");
        }

        var poll = await _pollRepository.GetByIdAsync(pollId, cancellationToken);
        if (poll == null)
        {
            throw new Domain.Exceptions.InvalidPollException($"Poll with ID {pollId} not found.");
        }

        // Validate that poll belongs to the event
        if (poll.EventId != eventId)
        {
            throw new Domain.Exceptions.InvalidPollException($"Poll with ID {pollId} does not belong to event with ID {eventId}.");
        }

        var totalVotes = await _pollVoteRepository.GetVoteCountByPollIdAsync(pollId, cancellationToken);
        var voteCountsByOption = await _pollVoteRepository.GetVoteCountsByOptionIndexAsync(pollId, cancellationToken);

        // Initialize all options with 0 votes
        var optionVotes = new Dictionary<string, int>();
        for (int i = 0; i < poll.Options.Count; i++)
        {
            optionVotes[i.ToString()] = voteCountsByOption.ContainsKey(i) ? voteCountsByOption[i] : 0;
        }

        return new PollResultsResponse
        {
            PollId = pollId,
            TotalVotes = totalVotes,
            OptionVotes = optionVotes
        };
    }
}
