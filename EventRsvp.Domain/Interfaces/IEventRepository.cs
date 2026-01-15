using EventRsvp.Domain.Entities;

namespace EventRsvp.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event> AddAsync(Event eventEntity, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Event> UpdateAsync(Event eventEntity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
