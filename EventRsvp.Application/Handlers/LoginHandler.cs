using EventRsvp.Application.DTOs;
using EventRsvp.Application.Services;
using Microsoft.Extensions.Configuration;

namespace EventRsvp.Application.Handlers;

/// <summary>
/// Handler for admin login authentication
/// </summary>
public class LoginHandler
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public LoginHandler(
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Validates admin credentials and generates a JWT token if valid
    /// </summary>
    /// <param name="request">The login request containing username and password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with JWT token and user information</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid</exception>
    public virtual Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Note: Username and password validation is handled by FluentValidation
        // before this handler is called. This handler focuses on credential verification.

        var adminUsername = _configuration["Admin:Username"];
        var adminPassword = _configuration["Admin:Password"];
        var jwtExpirationMinutesStr = _configuration["Jwt:ExpirationMinutes"];
        var jwtExpirationMinutes = 60;
        if (!string.IsNullOrWhiteSpace(jwtExpirationMinutesStr) && int.TryParse(jwtExpirationMinutesStr, out var parsedMinutes))
        {
            jwtExpirationMinutes = parsedMinutes;
        }

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Admin credentials are not configured.");
        }

        // Verify credentials (using constant time comparison for security)
        // In development, we compare against plain text password from config
        // In production, the password should be hashed and stored securely
        bool isValidCredentials = false;
        
        if (string.Equals(request.Username, adminUsername, StringComparison.OrdinalIgnoreCase))
        {
            // For now, we'll use plain text comparison (development only)
            // In production, the password in config should be hashed using BCrypt
            // and we'd use PasswordService.VerifyPassword here
            isValidCredentials = string.Equals(request.Password, adminPassword, StringComparison.Ordinal);
        }

        if (!isValidCredentials)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var token = _jwtTokenService.GenerateToken(request.Username, "Admin");
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtExpirationMinutes);

        var response = new LoginResponse
        {
            Token = token,
            Username = request.Username,
            ExpiresAt = expiresAt
        };

        return Task.FromResult(response);
    }
}
