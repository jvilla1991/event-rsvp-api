using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventRsvp.Infrastructure.Data.Repositories;

public class InviteRepository : IInviteRepository
{
    private readonly EventRsvpDbContext _context;

    public InviteRepository(EventRsvpDbContext context)
    {
        _context = context;
    }

    public async Task<Invite> AddAsync(Invite invite, CancellationToken cancellationToken = default)
    {
        _context.Invites.Add(invite);
        await _context.SaveChangesAsync(cancellationToken);
        return invite;
    }

    public async Task<Invite?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Invites
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Invites
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<Invite>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Invites
            .Where(i => i.EventId == eventId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invite?> GetByEventIdAndNameAsync(int eventId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Invites
            .FirstOrDefaultAsync(i => i.EventId == eventId &&
                i.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task<Invite> UpdateAsync(Invite invite, CancellationToken cancellationToken = default)
    {
        _context.Invites.Update(invite);

        // Explicitly mark Status as modified so EF Core always includes it in the
        // UPDATE statement. Without this, EF Core's ValueGeneratedOnAdd behaviour
        // (triggered by HasDefaultValue on int columns) can treat the value 0 as a
        // sentinel and silently omit it from the generated SQL.
        _context.Entry(invite).Property(i => i.Status).IsModified = true;

        await _context.SaveChangesAsync(cancellationToken);
        return invite;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var invite = await _context.Invites.FindAsync(new object[] { id }, cancellationToken);
        if (invite == null)
            return false;

        _context.Invites.Remove(invite);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
