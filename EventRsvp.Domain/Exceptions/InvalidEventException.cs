namespace EventRsvp.Domain.Exceptions;

public class InvalidEventException : DomainException
{
    public InvalidEventException(string message) : base(message)
    {
    }

    public InvalidEventException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
