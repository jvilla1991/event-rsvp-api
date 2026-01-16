using EventRsvp.Api.Helpers;
using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
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

    public EventsController(
        GetEventsHandler getEventsHandler,
        GetEventHandler getEventHandler,
        CreateEventHandler createEventHandler,
        UpdateEventHandler updateEventHandler,
        DeleteEventHandler deleteEventHandler)
    {
        _getEventsHandler = getEventsHandler;
        _getEventHandler = getEventHandler;
        _createEventHandler = createEventHandler;
        _updateEventHandler = updateEventHandler;
        _deleteEventHandler = deleteEventHandler;
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
        catch
        {
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
                return ErrorResponseHelper.NotFoundResponse($"Event with ID {id} not found.");
            }

            return Ok(eventResponse);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    /// <param name="request">Event creation data</param>
    /// <returns>Created event</returns>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        if (request == null)
        {
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");
        }

        try
        {
            var response = await _createEventHandler.HandleAsync(request);
            return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
        }
        catch
        {
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
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        if (request == null)
        {
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");
        }

        try
        {
            var response = await _updateEventHandler.HandleAsync(id, request);

            if (response == null)
            {
                return ErrorResponseHelper.NotFoundResponse($"Event with ID {id} not found.");
            }

            return Ok(response);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Delete an event. This will also delete all associated RSVPs.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        if (id <= 0)
        {
            return ErrorResponseHelper.BadRequestResponse("Event ID must be greater than zero.");
        }

        try
        {
            var deleted = await _deleteEventHandler.HandleAsync(id);

            if (!deleted)
            {
                return ErrorResponseHelper.NotFoundResponse($"Event with ID {id} not found.");
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return ErrorResponseHelper.BadRequestResponse(ex.Message);
        }
        catch
        {
            throw;
        }
    }
}
