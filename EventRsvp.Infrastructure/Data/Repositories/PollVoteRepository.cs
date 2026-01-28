using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventRsvp.Infrastructure.Data.Repositories;

public class PollVoteRepository : IPollVoteRepository
{
    private readonly EventRsvpDbContext _context;

    public PollVoteRepository(EventRsvpDbContext context)
    {
        _context = context;
    }

    public async Task<PollVote> AddAsync(PollVote pollVote, CancellationToken cancellationToken = default)
    {
        _context.PollVotes.Add(pollVote);
        await _context.SaveChangesAsync(cancellationToken);
        return pollVote;
    }

    public async Task<IEnumerable<PollVote>> GetByPollIdAsync(int pollId, CancellationToken cancellationToken = default)
    {
        return await _context.PollVotes
            .Where(v => v.PollId == pollId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteByPollIdAsync(int pollId, CancellationToken cancellationToken = default)
    {
        var votes = await _context.PollVotes
            .Where(v => v.PollId == pollId)
            .ToListAsync(cancellationToken);

        _context.PollVotes.RemoveRange(votes);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetVoteCountByPollIdAsync(int pollId, CancellationToken cancellationToken = default)
    {
        return await _context.PollVotes
            .CountAsync(v => v.PollId == pollId, cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetVoteCountsByOptionIndexAsync(int pollId, CancellationToken cancellationToken = default)
    {
        var votes = await _context.PollVotes
            .Where(v => v.PollId == pollId)
            .ToListAsync(cancellationToken);

        var counts = new Dictionary<int, int>();

        foreach (var vote in votes)
        {
            foreach (var optionIndex in vote.SelectedOptions)
            {
                if (counts.ContainsKey(optionIndex))
                {
                    counts[optionIndex]++;
                }
                else
                {
                    counts[optionIndex] = 1;
                }
            }
        }

        return counts;
    }
}
