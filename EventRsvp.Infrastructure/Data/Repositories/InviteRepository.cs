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
        // Change-tracker path. This was previously ExecuteUpdateAsync to work around
        // EF Core's ValueGeneratedOnAdd sentinel dropping Status from UPDATE statements
        // when it had a DB default of 0. The FixInviteStatusValueGenerated migration
        // removed that default, so EF Core no longer treats Status=0 as a sentinel and
        // the standard change-tracker UPDATE includes it correctly.
        // The change-tracker path also works with the in-memory provider used in tests.
        _context.Invites.Update(invite);
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
