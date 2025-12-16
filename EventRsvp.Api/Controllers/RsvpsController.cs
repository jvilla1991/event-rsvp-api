using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RsvpsController : ControllerBase
{
    private readonly CreateRsvpHandler _createRsvpHandler;
    private readonly GetRsvpsHandler _getRsvpsHandler;
    private readonly ILogger<RsvpsController> _logger;

    public RsvpsController(
        CreateRsvpHandler createRsvpHandler,
        GetRsvpsHandler getRsvpsHandler,
        ILogger<RsvpsController> logger)
    {
        _createRsvpHandler = createRsvpHandler;
        _getRsvpsHandler = getRsvpsHandler;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RsvpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RsvpResponse>> CreateRsvp([FromBody] CreateRsvpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _createRsvpHandler.HandleAsync(request);
            return CreatedAtAction(nameof(GetRsvps), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP");
            throw;
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RsvpResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RsvpResponse>>> GetRsvps()
    {
        try
        {
            var rsvps = await _getRsvpsHandler.HandleAsync();
            return Ok(rsvps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving RSVPs");
            throw;
        }
    }
}

