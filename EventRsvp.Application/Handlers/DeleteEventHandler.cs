using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

/// <summary>
/// Handler for deleting events
/// </summary>
public class DeleteEventHandler
{
    private readonly IEventRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteEventHandler"/> class
    /// </summary>
    /// <param name="repository">The event repository</param>
    /// <exception cref="ArgumentNullException">Thrown when repository is null</exception>
    public DeleteEventHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Deletes an event by ID
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

        return await _repository.DeleteAsync(id, cancellationToken);
    }
}
