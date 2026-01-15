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
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        GetEventsHandler getEventsHandler,
        GetEventHandler getEventHandler,
        ILogger<EventsController> logger)
    {
        _getEventsHandler = getEventsHandler;
        _getEventHandler = getEventHandler;
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
}
