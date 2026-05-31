using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventRsvp.Infrastructure.Data.Repositories;

public class RsvpRepository : IRsvpRepository
{
    private readonly EventRsvpDbContext _context;

    public RsvpRepository(EventRsvpDbContext context)
    {
        _context = context;
    }

    public async Task<Rsvp> AddAsync(Rsvp rsvp, CancellationToken cancellationToken = default)
    {
        _context.Rsvps.Add(rsvp);
        await _context.SaveChangesAsync(cancellationToken);
        return rsvp;
    }

    public async Task<Rsvp> UpdateAsync(Rsvp rsvp, CancellationToken cancellationToken = default)
    {
        _context.Rsvps.Update(rsvp);
        await _context.SaveChangesAsync(cancellationToken);
        return rsvp;
    }

    public async Task<Rsvp?> GetByEventIdAndNameAsync(int eventId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Rsvps
            .FirstOrDefaultAsync(r => r.EventId == eventId &&
                r.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task<IEnumerable<Rsvp>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rsvps
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Rsvp>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Rsvps
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var rsvps = await _context.Rsvps
            .Where(r => r.EventId == eventId)
            .ToListAsync(cancellationToken);
        
        _context.Rsvps.RemoveRange(rsvps);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
