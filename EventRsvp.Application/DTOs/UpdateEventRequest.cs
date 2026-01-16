namespace EventRsvp.Application.DTOs;

/// <summary>
/// Request DTO for updating an existing event
/// </summary>
public class UpdateEventRequest
{
    /// <summary>
    /// The title of the event (required, max 200 characters)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the event (max 1000 characters)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional date and time of the event
    /// </summary>
    public DateTime? EventDateTime { get; set; }

    /// <summary>
    /// Optional address where the event will take place (max 500 characters)
    /// </summary>
    public string? Address { get; set; }
}
