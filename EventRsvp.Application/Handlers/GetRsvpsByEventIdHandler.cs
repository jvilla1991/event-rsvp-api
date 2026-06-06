using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetRsvpsByEventIdHandler
{
    private readonly IRsvpRepository _repository;

    public GetRsvpsByEventIdHandler(IRsvpRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RsvpResponse>> HandleAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var rsvps = await _repository.GetByEventIdAsync(eventId, cancellationToken);

        return rsvps
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RsvpResponse
            {
                Id = r.Id,
                Name = r.Name,
                Status = r.Status.ToString(),
                ProposedTime = r.ProposedTime,
                CreatedAt = r.CreatedAt
            });
    }
}
