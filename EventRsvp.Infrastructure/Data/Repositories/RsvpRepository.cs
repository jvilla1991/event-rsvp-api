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

    public async Task<IEnumerable<Rsvp>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rsvps
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

