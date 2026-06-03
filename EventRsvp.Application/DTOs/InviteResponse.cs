namespace EventRsvp.Application.DTOs;

/// <summary>
/// Response DTO for invite data
/// </summary>
public class InviteResponse
{
    public int Id { get; set; }
    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Current invite status: NotOpened, Opened, Accepted, or Declined
    /// </summary>
    public string Status { get; set; } = "NotOpened";

    public DateTime? ViewedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
