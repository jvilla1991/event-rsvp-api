using EventRsvp.Api.Helpers;
using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

/// <summary>
/// Controller for managing polls for events
/// </summary>
[ApiController]
[Route("api/events/{eventId}/polls")]
[ApiExplorerSettings(GroupName = "Polls")]
public class PollsController : ControllerBase
{
    private readonly GetPollsByEventIdHandler _getPollsByEventIdHandler;
    private readonly CreatePollHandler _createPollHandler;
    private readonly UpdatePollHandler _updatePollHandler;
    private readonly DeletePollHandler _deletePollHandler;
    private readonly SubmitPollVoteHandler _submitPollVoteHandler;
    private readonly GetPollResultsHandler _getPollResultsHandler;

    public PollsController(
        GetPollsByEventIdHandler getPollsByEventIdHandler,
        CreatePollHandler createPollHandler,
        UpdatePollHandler updatePollHandler,
        DeletePollHandler deletePollHandler,
        SubmitPollVoteHandler submitPollVoteHandler,
        GetPollResultsHandler getPollResultsHandler)
    {
        _getPollsByEventIdHandler = getPollsByEventIdHandler;
        _createPollHandler = createPollHandler;
        _updatePollHandler = updatePollHandler;
        _deletePollHandler = deletePollHandler;
        _submitPollVoteHandler = submitPollVoteHandler;
        _getPollResultsHandler = getPollResultsHandler;
    }

    /// <summary>
    /// Get all polls for a specific event
    /// </summary>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <returns>List of polls for the event</returns>
    /// <response code="200">Returns the list of polls</response>
    /// <response code="400">Invalid event ID or event not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PollResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<PollResponse>>> GetPolls(int eventId)
    {
        try
        {
            var polls = await _getPollsByEventIdHandler.HandleAsync(eventId);
            return Ok(polls);
        }
        catch (Domain.Exceptions.InvalidPollException ex)
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
    /// Create a new poll for an event
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. Creates a new poll for the specified event.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="request">The poll request data containing question, options, and allowMultiple</param>
    /// <returns>The created poll with assigned ID</returns>
    /// <response code="201">Poll created successfully</response>
    /// <response code="400">Invalid request data or event not found</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(PollResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PollResponse>> CreatePoll(int eventId, [FromBody] CreatePollRequest request)
    {
        if (request == null)
        {
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");
        }

        try
        {
            var response = await _createPollHandler.HandleAsync(eventId, request);
            return CreatedAtAction(nameof(GetPolls), new { eventId = eventId }, response);
        }
        catch (Domain.Exceptions.InvalidPollException ex)
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
    /// Update an existing poll
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. Updates the poll with the specified ID.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="pollId">The unique identifier of the poll</param>
    /// <param name="request">The poll update data</param>
    /// <returns>The updated poll</returns>
    /// <response code="200">Poll updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Poll not found</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
    [HttpPut("{pollId}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(PollResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PollResponse>> UpdatePoll(int eventId, int pollId, [FromBody] UpdatePollRequest request)
    {
        if (request == null)
        {
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");
        }

        try
        {
            var response = await _updatePollHandler.HandleAsync(eventId, pollId, request);

            if (response == null)
            {
                return ErrorResponseHelper.NotFoundResponse($"Poll with ID {pollId} not found.");
            }

            return Ok(response);
        }
        catch (Domain.Exceptions.InvalidPollException ex)
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
    /// Delete a poll and all its votes
    /// </summary>
    /// <remarks>
    /// Requires admin authentication. Deletes the poll with the specified ID and all its votes.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="pollId">The unique identifier of the poll</param>
    /// <returns>Success message</returns>
    /// <response code="200">Poll deleted successfully</response>
    /// <response code="400">Invalid poll ID</response>
    /// <response code="404">Poll not found</response>
    /// <response code="401">Unauthorized - Admin authentication required</response>
    [HttpDelete("{pollId}")]
    [Authorize(Policy = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeletePoll(int eventId, int pollId)
    {
        if (pollId <= 0)
        {
            return ErrorResponseHelper.BadRequestResponse("Poll ID must be greater than zero.");
        }

        try
        {
            var deleted = await _deletePollHandler.HandleAsync(eventId, pollId);

            if (!deleted)
            {
                return ErrorResponseHelper.NotFoundResponse($"Poll with ID {pollId} not found.");
            }

            return Ok(new { message = "Poll deleted successfully" });
        }
        catch (Domain.Exceptions.InvalidPollException ex)
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
    /// Submit a vote on a poll
    /// </summary>
    /// <remarks>
    /// Public endpoint (no authentication required). Submits a vote on the specified poll.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="pollId">The unique identifier of the poll</param>
    /// <param name="request">The vote request data containing selectedOptions</param>
    /// <returns>Success message</returns>
    /// <response code="200">Vote submitted successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Poll not found</response>
    [HttpPost("{pollId}/votes")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SubmitVote(int eventId, int pollId, [FromBody] SubmitVoteRequest request)
    {
        if (request == null)
        {
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");
        }

        try
        {
            await _submitPollVoteHandler.HandleAsync(eventId, pollId, request);
            return Ok(new { message = "Vote submitted successfully" });
        }
        catch (Domain.Exceptions.InvalidPollException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return ErrorResponseHelper.NotFoundResponse(ex.Message);
            }
            return ErrorResponseHelper.BadRequestResponse(ex.Message);
        }
        catch (Domain.Exceptions.InvalidPollVoteException ex)
        {
            return ErrorResponseHelper.BadRequestResponse(ex.Message);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Get poll results with vote counts
    /// </summary>
    /// <remarks>
    /// Public endpoint (no authentication required). Returns poll results with vote counts.
    /// </remarks>
    /// <param name="eventId">The unique identifier of the event</param>
    /// <param name="pollId">The unique identifier of the poll</param>
    /// <returns>Poll results with vote counts</returns>
    /// <response code="200">Returns the poll results</response>
    /// <response code="404">Poll not found</response>
    [HttpGet("{pollId}/results")]
    [ProducesResponseType(typeof(PollResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PollResultsResponse>> GetPollResults(int eventId, int pollId)
    {
        try
        {
            var results = await _getPollResultsHandler.HandleAsync(eventId, pollId);
            return Ok(results);
        }
        catch (Domain.Exceptions.InvalidPollException ex)
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
}
