using EventRsvp.Api.Helpers;
using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

/// <summary>
/// Controller for managing RSVPs for events
/// </summary>
[ApiController]
[Route("api/events/{eventId}/rsvps")]
[ApiExplorerSettings(GroupName = "RSVPs")]
public class RsvpsController : ControllerBase
{
    private readonly CreateRsvpHandler _createRsvpHandler;
    private readonly GetRsvpsByEventIdHandler _getRsvpsByEventIdHandler;
    private readonly GetAttendanceByEventIdHandler _getAttendanceByEventIdHandler;

    public RsvpsController(
        CreateRsvpHandler createRsvpHandler,
        GetRsvpsByEventIdHandler getRsvpsByEventIdHandler,
        GetAttendanceByEventIdHandler getAttendanceByEventIdHandler)
    {
        _createRsvpHandler = createRsvpHandler;
        _getRsvpsByEventIdHandler = getRsvpsByEventIdHandler;
        _getAttendanceByEventIdHandler = getAttendanceByEventIdHandler;
    }

    /// <summary>
    /// Creates a new RSVP for the specified event
    /// </summary>
    /// <remarks>
    /// Creates a new RSVP (Response to Invitation) for the specified event. 
    /// The RSVP includes the attendee's name and whether they will attend.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="request">The RSVP request data containing name and attendance status</param>
    /// <returns>The created RSVP with assigned ID</returns>
    /// <response code="201">RSVP created successfully</response>
    /// <response code="400">Invalid request data or event not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(RsvpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RsvpResponse>> CreateRsvp(int eventId, [FromBody] CreateRsvpRequest request)
    {
        if (request == null)
        {
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");
        }

        try
        {
            var response = await _createRsvpHandler.HandleAsync(eventId, request);
            return CreatedAtAction(nameof(GetRsvps), new { eventId = eventId }, response);
        }
        catch (Domain.Exceptions.InvalidRsvpException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return ErrorResponseHelper.NotFoundResponse(ex.Message);
            }
            return ErrorResponseHelper.BadRequestResponse(ex.Message);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Gets all RSVPs for the specified event
    /// </summary>
    /// <remarks>
    /// Retrieves all RSVPs (Responses to Invitations) for a specific event.
    /// Returns an empty list if the event has no RSVPs or if the event doesn't exist.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <returns>List of RSVPs for the specified event</returns>
    /// <response code="200">Returns the list of RSVPs for the event</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RsvpResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RsvpResponse>>> GetRsvps(int eventId)
    {
        try
        {
            var rsvps = await _getRsvpsByEventIdHandler.HandleAsync(eventId);
            return Ok(rsvps);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Gets the unified attendance list for the specified event
    /// </summary>
    /// <remarks>
    /// Returns a combined view of all invited people and direct RSVPs.
    /// Invited people appear as NotOpened or Opened until they respond,
    /// then transition to Accepted or Declined. People who RSVP without
    /// an invite appear directly as Accepted or Declined.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <returns>Unified attendance list for the specified event</returns>
    /// <response code="200">Returns the attendance list</response>
    [HttpGet("attendance")]
    [ProducesResponseType(typeof(IEnumerable<AttendanceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AttendanceResponse>>> GetAttendance(int eventId)
    {
        try
        {
            var attendance = await _getAttendanceByEventIdHandler.HandleAsync(eventId);
            return Ok(attendance);
        }
        catch
        {
            throw;
        }
    }
}

