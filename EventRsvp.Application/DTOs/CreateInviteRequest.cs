namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for creating a shareable invite link
/// </summary>
public class CreateInviteRequest
{
    /// <summary>
    /// The name of the person being invited (e.g. "Bob")
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
