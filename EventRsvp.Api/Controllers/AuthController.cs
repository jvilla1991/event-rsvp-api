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
