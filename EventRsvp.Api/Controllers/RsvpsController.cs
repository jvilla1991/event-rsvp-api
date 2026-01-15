using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

[ApiController]
[Route("api/events/{eventId}/rsvps")]
public class RsvpsController : ControllerBase
{
    private readonly CreateRsvpHandler _createRsvpHandler;
    private readonly GetRsvpsByEventIdHandler _getRsvpsByEventIdHandler;
    private readonly ILogger<RsvpsController> _logger;

    public RsvpsController(
        CreateRsvpHandler createRsvpHandler,
        GetRsvpsByEventIdHandler getRsvpsByEventIdHandler,
        ILogger<RsvpsController> logger)
    {
        _createRsvpHandler = createRsvpHandler;
        _getRsvpsByEventIdHandler = getRsvpsByEventIdHandler;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new RSVP for the specified event
    /// </summary>
    /// <param name="eventId">The ID of the event</param>
    /// <param name="request">The RSVP request data</param>
    /// <returns>The created RSVP</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RsvpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RsvpResponse>> CreateRsvp(int eventId, [FromBody] CreateRsvpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _createRsvpHandler.HandleAsync(eventId, request);
            return CreatedAtAction(nameof(GetRsvps), new { eventId = eventId }, response);
        }
        catch (Domain.Exceptions.InvalidRsvpException ex)
        {
            _logger.LogWarning(ex, "Invalid RSVP creation attempt for event {EventId}", eventId);
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP for event {EventId}", eventId);
            throw;
        }
    }

    /// <summary>
    /// Gets all RSVPs for the specified event
    /// </summary>
    /// <param name="eventId">The ID of the event</param>
    /// <returns>List of RSVPs for the event</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RsvpResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RsvpResponse>>> GetRsvps(int eventId)
    {
        try
        {
            var rsvps = await _getRsvpsByEventIdHandler.HandleAsync(eventId);
            return Ok(rsvps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving RSVPs for event {EventId}", eventId);
            throw;
        }
    }
}

