using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

/// <summary>
/// Controller for authentication endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;
    private readonly ILogger<AuthController> _logger;

    public AuthController(LoginHandler loginHandler, ILogger<AuthController> logger)
    {
        _loginHandler = loginHandler ?? throw new ArgumentNullException(nameof(loginHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticate admin user and receive JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("Login attempted with null request");
            return BadRequest(new { error = "Request body is required." });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();
            
            _logger.LogWarning("Login validation failed: {Errors}", string.Join(", ", errors));
            return BadRequest(new { error = "Validation failed.", errors = errors });
        }

        try
        {
            _logger.LogInformation("Login attempt for username: {Username}", request.Username);
            var response = await _loginHandler.HandleAsync(request);
            _logger.LogInformation("Login successful for username: {Username}", request.Username);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for username: {Username}", request.Username);
            throw;
        }
    }
}
