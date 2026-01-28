namespace EventRsvp.Application.DTOs;

/// <summary>
/// Response DTO for poll results
/// </summary>
public class PollResultsResponse
{
    public int PollId { get; set; }
    public int TotalVotes { get; set; }
    public Dictionary<string, int> OptionVotes { get; set; } = new();
}
