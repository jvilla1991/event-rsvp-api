namespace EventRsvp.Application.Services;

/// <summary>
/// Service interface for JWT token generation and validation.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the specified username and role.
    /// </summary>
    /// <param name="username">The username to include in the token claims.</param>
    /// <param name="role">The role to include in the token claims.</param>
    /// <returns>The generated JWT token string.</returns>
    string GenerateToken(string username, string role);

    /// <summary>
    /// Validates a JWT token and extracts the principal.
    /// </summary>
    /// <param name="token">The JWT token string to validate.</param>
    /// <returns>The principal if the token is valid; otherwise, null.</returns>
    System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
}
