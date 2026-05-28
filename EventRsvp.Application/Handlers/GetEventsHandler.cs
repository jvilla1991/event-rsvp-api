using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetEventsHandler
{
    private readonly IEventRepository _repository;

    public GetEventsHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<EventResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetAllAsync(cancellationToken);

        return events.Select(e => new EventResponse
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            EventDateTime = e.EventDateTime,
            Address = e.Address,
            AllowTimeProposal = e.AllowTimeProposal,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });
    }
}
