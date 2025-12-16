namespace EventRsvp.Domain.Exceptions;

public class InvalidRsvpException : DomainException
{
    public InvalidRsvpException(string message) : base(message)
    {
    }

    public InvalidRsvpException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

