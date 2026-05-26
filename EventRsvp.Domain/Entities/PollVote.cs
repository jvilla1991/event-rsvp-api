using EventRsvp.Domain.Exceptions;

namespace EventRsvp.Domain.Entities;

public class PollVote
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public string VoterName { get; set; } = string.Empty;
    public List<int> SelectedOptions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (PollId <= 0)
        {
            throw new InvalidPollVoteException("Poll ID is required and must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(VoterName))
        {
            throw new InvalidPollVoteException("Voter name is required and cannot be empty.");
        }

        if (SelectedOptions == null || SelectedOptions.Count == 0)
        {
            throw new InvalidPollVoteException("At least one option must be selected.");
        }

        if (SelectedOptions.Any(idx => idx < 0))
        {
            throw new InvalidPollVoteException("Selected option indices cannot be negative.");
        }
    }
}
