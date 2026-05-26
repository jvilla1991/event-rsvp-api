namespace EventRsvp.Domain.Exceptions;

public class InvalidPollException : DomainException
{
    public InvalidPollException(string message) : base(message)
    {
    }
}
