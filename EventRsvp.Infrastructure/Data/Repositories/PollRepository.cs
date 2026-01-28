using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventRsvp.Infrastructure.Data.Repositories;

public class PollRepository : IPollRepository
{
    private readonly EventRsvpDbContext _context;

    public PollRepository(EventRsvpDbContext context)
    {
        _context = context;
    }

    public async Task<Poll> AddAsync(Poll poll, CancellationToken cancellationToken = default)
    {
        _context.Polls.Add(poll);
        await _context.SaveChangesAsync(cancellationToken);
        return poll;
    }

    public async Task<Poll?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Polls
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Poll>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Polls
            .Where(p => p.EventId == eventId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Poll> UpdateAsync(Poll poll, CancellationToken cancellationToken = default)
    {
        poll.UpdatedAt = DateTime.UtcNow;
        _context.Polls.Update(poll);
        await _context.SaveChangesAsync(cancellationToken);
        return poll;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var poll = await _context.Polls.FindAsync(new object[] { id }, cancellationToken);
        if (poll == null)
        {
            return false;
        }

        _context.Polls.Remove(poll);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var polls = await _context.Polls
            .Where(p => p.EventId == eventId)
            .ToListAsync(cancellationToken);

        _context.Polls.RemoveRange(polls);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
