namespace EventRsvp.Application.DTOs;

/// <summary>
/// Response DTO for invite data
/// </summary>
public class InviteResponse
{
    public int Id { get; set; }
    public int EventId { get; set; }

    /// <summary>
    /// The name of the person this invite was sent to
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique token used to build the shareable link
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Whether the recipient has opened the invite link
    /// </summary>
    public bool IsViewed => ViewedAt.HasValue;

    /// <summary>
    /// UTC timestamp when the invite was first opened; null if not yet viewed
    /// </summary>
    public DateTime? ViewedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
