using EventRsvp.Domain.Entities;

namespace EventRsvp.Domain.Interfaces;

public interface IPollVoteRepository
{
    Task<PollVote> AddAsync(PollVote pollVote, CancellationToken cancellationToken = default);
    Task<IEnumerable<PollVote>> GetByPollIdAsync(int pollId, CancellationToken cancellationToken = default);
    Task<bool> DeleteByPollIdAsync(int pollId, CancellationToken cancellationToken = default);
    Task<int> GetVoteCountByPollIdAsync(int pollId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetVoteCountsByOptionIndexAsync(int pollId, CancellationToken cancellationToken = default);
}
