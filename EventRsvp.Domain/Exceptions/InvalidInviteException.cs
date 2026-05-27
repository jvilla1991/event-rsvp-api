namespace EventRsvp.Domain.Exceptions;

public class InvalidInviteException : DomainException
{
    public InvalidInviteException(string message) : base(message)
    {
    }
}
