using EventRsvp.Application.DTOs;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;

namespace EventRsvp.Application.Handlers;

public class CreateRsvpHandler
{
    private readonly IRsvpRepository _repository;

    public CreateRsvpHandler(IRsvpRepository repository)
    {
        _repository = repository;
    }

    public async Task<RsvpResponse> HandleAsync(CreateRsvpRequest request, CancellationToken cancellationToken = default)
    {
        var rsvp = new Rsvp
        {
            Name = request.Name.Trim(),
            BringingDish = request.BringingDish,
            Dishes = request.BringingDish 
                ? request.Dishes.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim()).ToList()
                : new List<string>(),
            WhiteElephant = request.WhiteElephant,
            CreatedAt = DateTime.UtcNow
        };

        rsvp.Validate();

        var createdRsvp = await _repository.AddAsync(rsvp, cancellationToken);

        return new RsvpResponse
        {
            Id = createdRsvp.Id,
            Name = createdRsvp.Name,
            BringingDish = createdRsvp.BringingDish,
            Dishes = createdRsvp.Dishes,
            WhiteElephant = createdRsvp.WhiteElephant,
            CreatedAt = createdRsvp.CreatedAt
        };
    }
}

