namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for creating a shareable invite link
/// </summary>
public class CreateInviteRequest
{
    /// <summary>
    /// Optional name to identify who this invite was sent to (e.g. "Bob").
    /// If omitted the invite is anonymous.
    /// </summary>
    public string? Name { get; set; }
}
