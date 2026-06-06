using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? EventDateTime { get; set; }
    public string? Address { get; set; }
    public bool AllowTimeProposal { get; set; }

    /// <summary>
    /// When true, public (non-admin) visitors may create polls on this event.
    /// </summary>
    public bool AllowGuestPolls { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidEventException("Title is required and cannot be empty.");
        }

        if (Title.Length > 200)
        {
            throw new InvalidEventException("Title cannot exceed 200 characters.");
        }
    }
}
