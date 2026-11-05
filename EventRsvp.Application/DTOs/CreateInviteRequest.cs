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

    /// <summary>
    /// When true, this recipient may bring one additional guest (a "+1").
    /// Controls whether the RSVP form shows guest-name fields. Defaults to false.
    /// </summary>
    public bool AllowGuest { get; set; }
}
