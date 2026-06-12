using EventRsvp.Api.Helpers;
using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
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
    private readonly DeleteAttendanceHandler _deleteAttendanceHandler;

    public RsvpsController(
        CreateRsvpHandler createRsvpHandler,
        GetRsvpsByEventIdHandler getRsvpsByEventIdHandler,
        GetAttendanceByEventIdHandler getAttendanceByEventIdHandler,
        DeleteAttendanceHandler deleteAttendanceHandler)
    {
        _createRsvpHandler = createRsvpHandler;
        _getRsvpsByEventIdHandler = getRsvpsByEventIdHandler;
        _getAttendanceByEventIdHandler = getAttendanceByEventIdHandler;
        _deleteAttendanceHandler = deleteAttendanceHandler;
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

    /// <summary>
    /// Removes a person from the event's attendance list
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. The attendance list merges tracked invites and
    /// walk-in RSVPs, so <paramref name="source"/> ("invite" or "rsvp") selects which
    /// record backs the row. Removing an invited person also deletes their matching
    /// RSVP so they don't reappear as a walk-in entry.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="source">The attendance row's source: "invite" or "rsvp"</param>
    /// <param name="id">The unique identifier of the backing invite or RSVP record</param>
    /// <returns>Success message</returns>
    /// <response code="200">Attendee removed successfully</response>
    /// <response code="400">Unknown source or the record belongs to a different event</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
    /// <response code="404">Event or attendee not found</response>
    [HttpDelete("attendance/{source}/{id}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAttendee(int eventId, string source, int id)
    {
        try
        {
            var deleted = await _deleteAttendanceHandler.HandleAsync(eventId, source, id);
            if (!deleted)
                return ErrorResponseHelper.NotFoundResponse($"Attendee not found for {source} ID {id}.");

            return Ok(new { message = "Attendee removed successfully" });
        }
        catch (Domain.Exceptions.InvalidRsvpException ex)
        {
            return ex.Message.Contains("not found")
                ? ErrorResponseHelper.NotFoundResponse(ex.Message)
                : ErrorResponseHelper.BadRequestResponse(ex.Message);
        }
        catch
        {
            throw;
        }
    }
}

