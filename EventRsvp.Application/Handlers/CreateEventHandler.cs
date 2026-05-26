using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class CreateEventHandler
{
    private readonly IEventRepository _repository;

    public CreateEventHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<EventResponse> HandleAsync(CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var eventEntity = new Event
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            EventDateTime = request.EventDateTime,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        eventEntity.Validate();

        var createdEvent = await _repository.AddAsync(eventEntity, cancellationToken);

        return new EventResponse
        {
            Id = createdEvent.Id,
            Title = createdEvent.Title,
            Description = createdEvent.Description,
            EventDateTime = createdEvent.EventDateTime,
            Address = createdEvent.Address,
            CreatedAt = createdEvent.CreatedAt,
            UpdatedAt = createdEvent.UpdatedAt
        };
    }
}
