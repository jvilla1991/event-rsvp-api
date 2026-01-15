using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class DeleteEventHandler
{
    private readonly IEventRepository _repository;

    public DeleteEventHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }
}
