using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetEventHandler
{
    private readonly IEventRepository _repository;
    private readonly IRsvpRepository _rsvpRepository;

    public GetEventHandler(IEventRepository repository, IRsvpRepository rsvpRepository)
    {
        _repository = repository;
        _rsvpRepository = rsvpRepository;
    }

    public async Task<EventResponse?> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken);

        if (eventEntity == null)
        {
            return null;
        }

        var attendingCount = await _rsvpRepository.GetYesCountByEventIdAsync(eventEntity.Id, cancellationToken);

        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            EventDateTime = eventEntity.EventDateTime,
            Address = eventEntity.Address,
            AllowTimeProposal = eventEntity.AllowTimeProposal,
            AllowGuestPolls = eventEntity.AllowGuestPolls,
            AttendingLimit = eventEntity.AttendingLimit,
            AttendingCount = attendingCount,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt
        };
    }
}
