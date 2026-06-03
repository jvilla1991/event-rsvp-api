namespace EventRsvp.Application.DTOs;

public class AttendanceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// NotOpened, Opened, Accepted, or Declined
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// True = attending, false = not attending, null = not yet responded
    /// </summary>
    public bool? WillAttend { get; set; }

    public DateTime? ProposedTime { get; set; }

    /// <summary>
    /// "invite" for tracked invitees, "rsvp" for walk-in responses
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
