using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class Rsvp
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool WillAttend { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (EventId <= 0)
        {
            throw new InvalidRsvpException("Event ID is required and must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidRsvpException("Name is required and cannot be empty.");
        }
    }
}
