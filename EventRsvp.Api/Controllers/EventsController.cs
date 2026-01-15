using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

/// <summary>
/// Controller for managing events
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly GetEventsHandler _getEventsHandler;
    private readonly GetEventHandler _getEventHandler;
    private readonly CreateEventHandler _createEventHandler;
    private readonly UpdateEventHandler _updateEventHandler;
    private readonly DeleteEventHandler _deleteEventHandler;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        GetEventsHandler getEventsHandler,
        GetEventHandler getEventHandler,
        CreateEventHandler createEventHandler,
        UpdateEventHandler updateEventHandler,
        DeleteEventHandler deleteEventHandler,
        ILogger<EventsController> logger)
    {
        _getEventsHandler = getEventsHandler;
        _getEventHandler = getEventHandler;
        _createEventHandler = createEventHandler;
        _updateEventHandler = updateEventHandler;
        _deleteEventHandler = deleteEventHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get all events
    /// </summary>
    /// <returns>List of all events</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents()
    {
        try
        {
            var events = await _getEventsHandler.HandleAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            throw;
        }
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Event details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(int id)
    {
        try
        {
            var eventResponse = await _getEventHandler.HandleAsync(id);

            if (eventResponse == null)
            {
                return NotFound(new { error = $"Event with ID {id} not found." });
            }

            return Ok(eventResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event with ID {EventId}", id);
            throw;
        }
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    /// <param name="request">Event creation data</param>
    /// <returns>Created event</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("CreateEvent called with null request");
            return BadRequest(new { error = "Request body is required." });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();
            
            _logger.LogWarning("CreateEvent validation failed: {Errors}", string.Join(", ", errors));
            return BadRequest(new { error = "Validation failed.", errors = errors });
        }

        try
        {
            _logger.LogInformation("Creating event with title: {Title}", request.Title);
            var response = await _createEventHandler.HandleAsync(request);
            _logger.LogInformation("Event created successfully with ID: {EventId}", response.Id);
            return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="request">Event update data</param>
    /// <returns>Updated event</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _updateEventHandler.HandleAsync(id, request);

            if (response == null)
            {
                return NotFound(new { error = $"Event with ID {id} not found." });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event with ID {EventId}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete an event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        try
        {
            var deleted = await _deleteEventHandler.HandleAsync(id);

            if (!deleted)
            {
                return NotFound(new { error = $"Event with ID {id} not found." });
            }

            _logger.LogInformation("Event deleted successfully with ID: {EventId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event with ID {EventId}", id);
            throw;
        }
    }
}
