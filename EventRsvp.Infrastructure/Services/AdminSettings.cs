namespace EventRsvp.Infrastructure.Services;

/// <summary>
/// Configuration settings for admin credentials
/// </summary>
public class AdminSettings
{
    public const string SectionName = "Admin";

    /// <summary>
    /// The admin username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The admin password (should be hashed in production, plain text for development only)
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
