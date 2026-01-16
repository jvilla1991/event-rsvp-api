namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for admin login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// The admin username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The admin password
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
