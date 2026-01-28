namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for creating a new poll
/// </summary>
/// <example>
/// {
///   "question": "What time works best for you?",
///   "options": ["Morning", "Afternoon", "Evening"],
///   "allowMultiple": false
/// }
/// </example>
public class CreatePollRequest
{
    /// <summary>
    /// The poll question (required, non-empty string)
    /// </summary>
    /// <example>What time works best for you?</example>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The poll options (required, array of at least 2 non-empty strings)
    /// </summary>
    /// <example>["Morning", "Afternoon", "Evening"]</example>
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// Whether multiple options can be selected (optional, defaults to false)
    /// </summary>
    /// <example>false</example>
    public bool AllowMultiple { get; set; } = false;
}
