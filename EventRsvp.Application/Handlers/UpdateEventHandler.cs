using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class UpdateEventHandler
{
    private readonly IEventRepository _repository;

    public UpdateEventHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<EventResponse?> HandleAsync(int id, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken);

        if (eventEntity == null)
        {
            return null;
        }

        eventEntity.Title = request.Title.Trim();
        eventEntity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        eventEntity.EventDateTime = request.EventDateTime;
        eventEntity.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        eventEntity.UpdatedAt = DateTime.UtcNow;

        eventEntity.Validate();

        var updatedEvent = await _repository.UpdateAsync(eventEntity, cancellationToken);

        return new EventResponse
        {
            Id = updatedEvent.Id,
            Title = updatedEvent.Title,
            Description = updatedEvent.Description,
            EventDateTime = updatedEvent.EventDateTime,
            Address = updatedEvent.Address,
            CreatedAt = updatedEvent.CreatedAt,
            UpdatedAt = updatedEvent.UpdatedAt
        };
    }
}
