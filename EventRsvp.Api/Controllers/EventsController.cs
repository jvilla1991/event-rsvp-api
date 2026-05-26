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
[ApiExplorerSettings(GroupName = "Events")]
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
    /// <returns>Returns a list of all events in the system</returns>
    /// <response code="200">Returns the list of events</response>
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
    /// <param name="id">The unique identifier of the event</param>
    /// <returns>Event details</returns>
    /// <response code="200">Returns the event with the specified ID</response>
    /// <response code="404">Event not found</response>
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
    /// <remarks>
    /// Requires admin authentication. Creates a new event with the provided details.
    /// </remarks>
    /// <param name="request">Event creation data including title, description, date/time, and address</param>
    /// <returns>The newly created event with assigned ID</returns>
    /// <response code="201">Event created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
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
    /// <remarks>
    /// Requires admin authentication. Updates the event with the specified ID.
    /// </remarks>
    /// <param name="id">The unique identifier of the event to update</param>
    /// <param name="request">Event update data</param>
    /// <returns>The updated event</returns>
    /// <response code="200">Event updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Event not found</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
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
    /// Delete an event
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. Deletes the event with the specified ID. 
    /// This operation will also delete all associated RSVPs due to cascade delete behavior.
    /// </remarks>
    /// <param name="id">The unique identifier of the event to delete</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Event deleted successfully</response>
    /// <response code="400">Invalid event ID</response>
    /// <response code="404">Event not found</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
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
