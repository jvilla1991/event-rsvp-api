using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class SubmitPollVoteHandler
{
    private readonly IPollRepository _pollRepository;
    private readonly IPollVoteRepository _pollVoteRepository;
    private readonly IEventRepository _eventRepository;

    public SubmitPollVoteHandler(
        IPollRepository pollRepository,
        IPollVoteRepository pollVoteRepository,
        IEventRepository eventRepository)
    {
        _pollRepository = pollRepository;
        _pollVoteRepository = pollVoteRepository;
        _eventRepository = eventRepository;
    }

    public async Task HandleAsync(int eventId, int pollId, SubmitVoteRequest request, CancellationToken cancellationToken = default)
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

        // Validate selected options
        if (!poll.AllowMultiple && request.SelectedOptions.Count != 1)
        {
            throw new Domain.Exceptions.InvalidPollVoteException("Exactly one option must be selected for this poll.");
        }

        if (poll.AllowMultiple && request.SelectedOptions.Count == 0)
        {
            throw new Domain.Exceptions.InvalidPollVoteException("At least one option must be selected.");
        }

        // Validate that all indices are within bounds
        var maxIndex = poll.Options.Count - 1;
        if (request.SelectedOptions.Any(idx => idx < 0 || idx > maxIndex))
        {
            throw new Domain.Exceptions.InvalidPollVoteException($"Selected option indices must be between 0 and {maxIndex}.");
        }

        // Note: voter_name is optional per requirements, but we'll use a placeholder or extract from context if needed
        // For now, we'll use "Anonymous" or allow the frontend to pass it
        var pollVote = new PollVote
        {
            PollId = pollId,
            VoterName = "Anonymous", // TODO: Could be extracted from request or context
            SelectedOptions = request.SelectedOptions,
            CreatedAt = DateTime.UtcNow
        };

        pollVote.Validate();

        await _pollVoteRepository.AddAsync(pollVote, cancellationToken);
    }
}
