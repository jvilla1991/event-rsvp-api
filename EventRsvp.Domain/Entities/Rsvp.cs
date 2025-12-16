using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class Rsvp
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool BringingDish { get; set; }
    public List<string> Dishes { get; set; } = new();
    public bool WhiteElephant { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidRsvpException("Name is required and cannot be empty.");
        }

        if (BringingDish && (Dishes == null || Dishes.Count == 0 || Dishes.All(d => string.IsNullOrWhiteSpace(d))))
        {
            throw new InvalidRsvpException("If bringing a dish, at least one dish name is required.");
        }
    }
}

