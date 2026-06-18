using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetEventsHandler
{
    private readonly IEventRepository _repository;
    private readonly IRsvpRepository _rsvpRepository;

    public GetEventsHandler(IEventRepository repository, IRsvpRepository rsvpRepository)
    {
        _repository = repository;
        _rsvpRepository = rsvpRepository;
    }

    public async Task<IEnumerable<EventResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetAllAsync(cancellationToken);
        // One grouped query for all events' "Yes" counts, rather than a count per event.
        var yesCounts = await _rsvpRepository.GetYesCountsByEventAsync(cancellationToken);

        return events.Select(e => new EventResponse
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            EventDateTime = e.EventDateTime,
            Address = e.Address,
            AllowTimeProposal = e.AllowTimeProposal,
            AllowGuestPolls = e.AllowGuestPolls,
            AttendingLimit = e.AttendingLimit,
            AttendingCount = yesCounts.TryGetValue(e.Id, out var count) ? count : 0,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });
    }
}
