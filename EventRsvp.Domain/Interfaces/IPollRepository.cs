using EventRsvp.Domain.Entities;

namespace EventRsvp.Domain.Interfaces;

public interface IPollRepository
{
    Task<Poll> AddAsync(Poll poll, CancellationToken cancellationToken = default);
    Task<Poll?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Poll>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Poll> UpdateAsync(Poll poll, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
}
