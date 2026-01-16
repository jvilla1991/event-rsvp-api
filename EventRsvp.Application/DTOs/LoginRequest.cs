namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for admin login
/// </summary>
/// <example>
/// {
///   "username": "admin",
///   "password": "admin123"
/// }
/// </example>
public class LoginRequest
{
    /// <summary>
    /// The admin username
    /// </summary>
    /// <example>admin</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The admin password
    /// </summary>
    /// <example>admin123</example>
    public string Password { get; set; } = string.Empty;
}
