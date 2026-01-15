using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventRsvp.Application.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventRsvp.Infrastructure.Services;

/// <summary>
/// JWT token service implementation for generating and validating JWT tokens.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Generates a JWT token for the specified username and role.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when username or role is null or empty.</exception>
    public string GenerateToken(string username, string role)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be null or empty.", nameof(role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, 
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token and extracts the principal.
    /// </summary>
    /// <param name="token">The JWT token string to validate.</param>
    /// <returns>The principal if the token is valid; otherwise, null.</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (SecurityTokenException)
        {
            // Token validation failed (expired, invalid signature, etc.)
            return null;
        }
        catch (Exception)
        {
            // Other exceptions (malformed token, etc.)
            return null;
        }
    }
}

/// <summary>
/// Configuration settings for JWT authentication.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The secret key used to sign JWT tokens. Must be at least 32 characters.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The issuer of the JWT token.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The intended audience of the JWT token.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The expiration time in minutes for JWT tokens.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
