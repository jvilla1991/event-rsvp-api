namespace EventRsvp.Application.DTOs;

/// <summary>
/// Response DTO for successful login containing JWT token and user information
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// The JWT authentication token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The authenticated username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The token expiration date and time
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
