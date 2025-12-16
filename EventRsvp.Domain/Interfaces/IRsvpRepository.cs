using EventRsvp.Domain.Entities;

namespace EventRsvp.Domain.Interfaces;

public interface IRsvpRepository
{
    Task<Rsvp> AddAsync(Rsvp rsvp, CancellationToken cancellationToken = default);
    Task<IEnumerable<Rsvp>> GetAllAsync(CancellationToken cancellationToken = default);
}

