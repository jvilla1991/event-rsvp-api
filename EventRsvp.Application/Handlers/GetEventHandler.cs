using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetEventHandler
{
    private readonly IEventRepository _repository;

    public GetEventHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<EventResponse?> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken);

        if (eventEntity == null)
        {
            return null;
        }

        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            EventDateTime = eventEntity.EventDateTime,
            Address = eventEntity.Address,
            AllowTimeProposal = eventEntity.AllowTimeProposal,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt
        };
    }
}
