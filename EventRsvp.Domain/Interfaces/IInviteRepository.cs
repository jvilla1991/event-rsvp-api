using EventRsvp.Domain.Entities;

namespace EventRsvp.Domain.Interfaces;

public interface IInviteRepository
{
    Task<Invite> AddAsync(Invite invite, CancellationToken cancellationToken = default);
    Task<Invite?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Invite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invite>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Invite> UpdateAsync(Invite invite, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
