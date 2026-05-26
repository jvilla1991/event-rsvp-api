namespace EventRsvp.Domain.Exceptions;

public class InvalidPollVoteException : DomainException
{
    public InvalidPollVoteException(string message) : base(message)
    {
    }
}
