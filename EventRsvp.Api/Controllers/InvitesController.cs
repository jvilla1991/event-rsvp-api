using EventRsvp.Api.Helpers;
using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

/// <summary>
/// Controller for managing event invites and tracking whether they have been viewed
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = "Invites")]
public class InvitesController : ControllerBase
{
    private readonly CreateInviteHandler _createInviteHandler;
    private readonly GetInvitesByEventIdHandler _getInvitesByEventIdHandler;
    private readonly ViewInviteHandler _viewInviteHandler;
    private readonly DeleteInviteHandler _deleteInviteHandler;

    public InvitesController(
        CreateInviteHandler createInviteHandler,
        GetInvitesByEventIdHandler getInvitesByEventIdHandler,
        ViewInviteHandler viewInviteHandler,
        DeleteInviteHandler deleteInviteHandler)
    {
        _createInviteHandler = createInviteHandler;
        _getInvitesByEventIdHandler = getInvitesByEventIdHandler;
        _viewInviteHandler = viewInviteHandler;
        _deleteInviteHandler = deleteInviteHandler;
    }

    // ── Admin routes (nested under events) ────────────────────────────────────

    /// <summary>
    /// Get all invites for a specific event
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. Returns all invites with their view status.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <returns>List of invites for the event</returns>
    /// <response code="200">Returns the list of invites</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
    /// <response code="404">Event not found</response>
    [HttpGet("api/events/{eventId}/invites")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<InviteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<InviteResponse>>> GetInvites(int eventId)
    {
        try
        {
            var invites = await _getInvitesByEventIdHandler.HandleAsync(eventId);
            return Ok(invites);
        }
        catch (InvalidInviteException ex)
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

    /// <summary>
    /// Create a new shareable invite for an event
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. Creates an invite for the named person and returns
    /// a unique token that can be embedded in a shareable link.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="request">The invite request containing the recipient's name</param>
    /// <returns>The created invite including the shareable token</returns>
    /// <response code="201">Invite created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
    /// <response code="404">Event not found</response>
    [HttpPost("api/events/{eventId}/invites")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(InviteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InviteResponse>> CreateInvite(int eventId, [FromBody] CreateInviteRequest request)
    {
        if (request == null)
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");

        try
        {
            var response = await _createInviteHandler.HandleAsync(eventId, request);
            return CreatedAtAction(nameof(GetInvites), new { eventId }, response);
        }
        catch (InvalidInviteException ex)
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

    /// <summary>
    /// Delete an invite
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. Deletes the invite with the specified ID.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="inviteId">The unique identifier of the invite</param>
    /// <returns>Success message</returns>
    /// <response code="200">Invite deleted successfully</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
    /// <response code="404">Invite not found</response>
    [HttpDelete("api/events/{eventId}/invites/{inviteId}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteInvite(int eventId, int inviteId)
    {
        try
        {
            var deleted = await _deleteInviteHandler.HandleAsync(eventId, inviteId);
            if (!deleted)
                return ErrorResponseHelper.NotFoundResponse($"Invite with ID {inviteId} not found.");

            return Ok(new { message = "Invite deleted successfully" });
        }
        catch (InvalidInviteException ex)
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

    // ── Public route (token-based, no auth required) ──────────────────────────

    /// <summary>
    /// View an invite by its unique token
    /// </summary>
    /// <remarks>
    /// Public endpoint — no authentication required. Records the first time the
    /// recipient opens the link (sets ViewedAt). Subsequent calls return the same
    /// invite without updating ViewedAt.
    /// </remarks>
    /// <param name="token">The unique invite token from the shareable link</param>
    /// <returns>Invite details including the event ID so the frontend can navigate</returns>
    /// <response code="200">Returns the invite (and marks it viewed if this is the first open)</response>
    /// <response code="404">No invite with the given token exists</response>
    [HttpGet("api/invites/{token}")]
    [ProducesResponseType(typeof(InviteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InviteResponse>> ViewInvite(string token)
    {
        try
        {
            var response = await _viewInviteHandler.HandleAsync(token);
            return Ok(response);
        }
        catch (InvalidInviteException ex)
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
