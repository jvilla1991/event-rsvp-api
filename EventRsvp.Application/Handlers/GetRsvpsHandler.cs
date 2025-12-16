using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class GetRsvpsHandler
{
    private readonly IRsvpRepository _repository;

    public GetRsvpsHandler(IRsvpRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RsvpResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var rsvps = await _repository.GetAllAsync(cancellationToken);

        return rsvps
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RsvpResponse
            {
                Id = r.Id,
                Name = r.Name,
                BringingDish = r.BringingDish,
                Dishes = r.Dishes,
                WhiteElephant = r.WhiteElephant,
                CreatedAt = r.CreatedAt
            });
    }
}

