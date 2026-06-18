using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class UpdateEventHandler
{
    private readonly IEventRepository _repository;
    private readonly IRsvpRepository _rsvpRepository;

    public UpdateEventHandler(IEventRepository repository, IRsvpRepository rsvpRepository)
    {
        _repository = repository;
        _rsvpRepository = rsvpRepository;
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
        eventEntity.AllowTimeProposal = request.AllowTimeProposal;
        eventEntity.AllowGuestPolls = request.AllowGuestPolls;
        eventEntity.AttendingLimit = request.AttendingLimit;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        eventEntity.Validate();

        var updatedEvent = await _repository.UpdateAsync(eventEntity, cancellationToken);
        var attendingCount = await _rsvpRepository.GetYesCountByEventIdAsync(updatedEvent.Id, cancellationToken);

        return new EventResponse
        {
            Id = updatedEvent.Id,
            Title = updatedEvent.Title,
            Description = updatedEvent.Description,
            EventDateTime = updatedEvent.EventDateTime,
            Address = updatedEvent.Address,
            AllowTimeProposal = updatedEvent.AllowTimeProposal,
            AllowGuestPolls = updatedEvent.AllowGuestPolls,
            AttendingLimit = updatedEvent.AttendingLimit,
            AttendingCount = attendingCount,
            CreatedAt = updatedEvent.CreatedAt,
            UpdatedAt = updatedEvent.UpdatedAt
        };
    }
}
