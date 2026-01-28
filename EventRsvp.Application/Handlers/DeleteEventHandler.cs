using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

/// <summary>
/// Handler for deleting events
/// </summary>
public class DeleteEventHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IRsvpRepository _rsvpRepository;
    private readonly IPollRepository _pollRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEventHandler"/> class
    /// </summary>
    /// <param name="eventRepository">The event repository</param>
    /// <param name="rsvpRepository">The RSVP repository</param>
    /// <param name="pollRepository">The poll repository</param>
    /// <exception cref="ArgumentNullException">Thrown when repository is null</exception>
    public DeleteEventHandler(IEventRepository eventRepository, IRsvpRepository rsvpRepository, IPollRepository pollRepository)
    {

        _eventRepository = eventRepository;
        _rsvpRepository = rsvpRepository;
        _pollRepository = pollRepository;
    }

    /// <summary>
    /// Deletes an event by ID. Also deletes all associated RSVPs and polls (which will cascade delete votes).
    /// </summary>
    /// <param name="id">The ID of the event to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the event was deleted successfully, false if the event was not found</returns>
    /// <exception cref="ArgumentException">Thrown when id is less than or equal to zero</exception>
    public async Task<bool> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Event ID must be greater than zero.", nameof(id));
        }

        var eventEntity = await _eventRepository.GetByIdAsync(id, cancellationToken);
        if (eventEntity == null)
        {
            return false;
        }

        await _rsvpRepository.DeleteByEventIdAsync(id, cancellationToken);
        
        // Delete associated polls (using Cascade, but deleting manually for consistency)
        // Votes will be cascade deleted when polls are deleted
        await _pollRepository.DeleteByEventIdAsync(id, cancellationToken);
        
        return await _eventRepository.DeleteAsync(id, cancellationToken);
    }
}
