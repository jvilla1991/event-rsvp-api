namespace EventRsvp.Application.DTOs;

public class EventResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? EventDateTime { get; set; }
    public string? Address { get; set; }
    public bool AllowTimeProposal { get; set; }
    public bool AllowGuestPolls { get; set; }

    /// <summary>
    /// Optional cap on the number of "Yes" RSVPs. Null means no limit.
    /// </summary>
    public int? AttendingLimit { get; set; }

    /// <summary>
    /// Current number of "Yes" RSVPs for this event. Used alongside
    /// <see cref="AttendingLimit"/> to show "X of Y attending" and to disable
    /// the "Yes" option once the cap is reached.
    /// </summary>
    public int AttendingCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
