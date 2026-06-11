using EventRsvp.Domain.Entities;

namespace EventRsvp.Domain.Interfaces;

public interface IRsvpRepository
{
    Task<Rsvp> AddAsync(Rsvp rsvp, CancellationToken cancellationToken = default);
    Task<Rsvp> UpdateAsync(Rsvp rsvp, CancellationToken cancellationToken = default);
    Task<IEnumerable<Rsvp>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Rsvp>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Rsvp?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Rsvp?> GetByEventIdAndNameAsync(int eventId, string name, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
}
