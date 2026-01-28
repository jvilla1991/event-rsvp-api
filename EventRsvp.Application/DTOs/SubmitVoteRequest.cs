namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for submitting a poll vote
/// </summary>
/// <example>
/// {
///   "selectedOptions": [0, 2]
/// }
/// </example>
public class SubmitVoteRequest
{
    /// <summary>
    /// Array of integers representing option indices (required)
    /// </summary>
    /// <example>[0, 2]</example>
    public List<int> SelectedOptions { get; set; } = new();
}
