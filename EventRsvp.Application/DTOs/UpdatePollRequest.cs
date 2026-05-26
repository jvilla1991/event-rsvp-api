namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for updating a poll
/// </summary>
public class UpdatePollRequest
{
    /// <summary>
    /// The poll question (required, non-empty string)
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The poll options (required, array of at least 2 non-empty strings)
    /// </summary>
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// Whether multiple options can be selected (optional, defaults to false)
    /// </summary>
    public bool AllowMultiple { get; set; } = false;
}
