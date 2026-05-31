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
        // Use ExecuteUpdateAsync (direct parameterised SQL) instead of the
        // change-tracker path. EF Core's sentinel/ValueGeneratedOnAdd logic
        // for int columns with default values can silently drop the Status
        // column from the generated UPDATE even when IsModified is forced.
        // ExecuteUpdateAsync bypasses all of that and always writes every
        // column we specify.
        var id       = invite.Id;
        var name     = invite.Name;
        var status   = invite.Status;
        var viewedAt = invite.ViewedAt;

        await _context.Invites
            .Where(i => i.Id == id)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(i => i.Name,     name)
                    .SetProperty(i => i.Status,   status)
                    .SetProperty(i => i.ViewedAt, viewedAt),
                cancellationToken);

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
