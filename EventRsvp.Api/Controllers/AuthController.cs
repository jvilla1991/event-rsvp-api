using EventRsvp.Api.Helpers;
using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace EventRsvp.Api.Controllers;

/// <summary>
/// Controller for authentication endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "Authentication")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;

    public AuthController(LoginHandler loginHandler)
    {
        _loginHandler = loginHandler ?? throw new ArgumentNullException(nameof(loginHandler));
    }

    /// <summary>
    /// Authenticate admin user and receive JWT token
    /// </summary>
    /// <remarks>
    /// Authenticates an admin user with username and password. Returns a JWT token that can be used 
    /// to access protected endpoints. The token expires after the configured time period (default: 60 minutes).
    /// 
    /// Use the returned token in the Authorization header as: "Bearer {token}"
    /// </remarks>
    /// <param name="request">Login credentials containing username and password</param>
    /// <returns>JWT token, username, and expiration information</returns>
    /// <response code="200">Authentication successful, returns JWT token</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (request == null)
        {
            return ErrorResponseHelper.BadRequestResponse("Request body is required.");
        }

        try
        {
            var response = await _loginHandler.HandleAsync(request);
            return Ok(response); 
        }
        catch (UnauthorizedAccessException ex)
        {
            return ErrorResponseHelper.UnauthorizedResponse(ex.Message);
        }
        catch
        {
            throw;
        }
    }
}
