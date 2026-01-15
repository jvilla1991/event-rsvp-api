using EventRsvp.Api.Controllers;
using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Net;

namespace EventRsvp.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<LoginHandler> _loginHandlerMock = null!;
    private Mock<ILogger<AuthController>> _loggerMock = null!;
    private AuthController _controller = null!;
    private const string ValidUsername = "admin";
    private const string ValidPassword = "admin123";
    private const string ValidToken = "valid-jwt-token";

    [SetUp]
    public void SetUp()
    {
        _loginHandlerMock = new Mock<LoginHandler>(Mock.Of<Application.Services.IJwtTokenService>(), Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_loginHandlerMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task Login_WhenValidCredentials_ShouldReturnOkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = ValidPassword
        };

        var expectedResponse = new LoginResponse
        {
            Token = ValidToken,
            Username = ValidUsername,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        _loginHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var loginResponse = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        
        loginResponse.Token.Should().Be(ValidToken);
        loginResponse.Username.Should().Be(ValidUsername);
        loginResponse.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        
        _loginHandlerMock.Verify(h => h.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Login_WhenInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = "wrongpassword"
        };

        _loginHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials."));

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var errorResponse = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
        
        _loginHandlerMock.Verify(h => h.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Login_WhenNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.Login(null!);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _loginHandlerMock.Verify(h => h.HandleAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Login_WhenModelStateInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = string.Empty,
            Password = ValidPassword
        };
        
        _controller.ModelState.AddModelError("Username", "Username is required.");

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _loginHandlerMock.Verify(h => h.HandleAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Login_WhenHandlerThrowsUnexpectedException_ShouldRethrow()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = ValidPassword
        };

        _loginHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act & Assert
        var act = async () => await _controller.Login(request);
        await act.Should().ThrowAsync<InvalidOperationException>();
        
        _loginHandlerMock.Verify(h => h.HandleAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
