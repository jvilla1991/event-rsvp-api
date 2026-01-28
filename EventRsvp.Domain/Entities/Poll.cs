using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class Poll
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public bool AllowMultiple { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (EventId <= 0)
        {
            throw new InvalidPollException("Event ID is required and must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Question))
        {
            throw new InvalidPollException("Question is required and cannot be empty.");
        }

        if (Options == null || Options.Count < 2)
        {
            throw new InvalidPollException("Poll must have at least 2 options.");
        }

        if (Options.Any(opt => string.IsNullOrWhiteSpace(opt)))
        {
            throw new InvalidPollException("All poll options must be non-empty strings.");
        }
    }
}
