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

    /// <summary>
    /// Counts the "Yes" RSVPs for a single event. Used to enforce and display the attending limit.
    /// </summary>
    Task<int> GetYesCountByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the "Yes" RSVP count per event, keyed by event id. Events with no "Yes"
    /// RSVPs are absent from the dictionary. Lets the list endpoint avoid an N+1 query.
    /// </summary>
    Task<IDictionary<int, int>> GetYesCountsByEventAsync(CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
}
