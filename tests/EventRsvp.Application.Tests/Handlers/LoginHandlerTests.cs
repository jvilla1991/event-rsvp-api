using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class LoginHandlerTests
{
    private Mock<IJwtTokenService> _jwtTokenServiceMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private LoginHandler _handler = null!;
    private const string ValidUsername = "admin";
    private const string ValidPassword = "admin123";
    private const string ValidToken = "valid-jwt-token";
    private const int JwtExpirationMinutes = 60;

    [SetUp]
    public void SetUp()
    {
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _configurationMock = new Mock<IConfiguration>();
        
        // Setup configuration mocks
        var adminSectionMock = new Mock<IConfigurationSection>();
        adminSectionMock.Setup(s => s["Username"]).Returns(ValidUsername);
        adminSectionMock.Setup(s => s["Password"]).Returns(ValidPassword);
        
        _configurationMock.Setup(c => c["Admin:Username"]).Returns(ValidUsername);
        _configurationMock.Setup(c => c["Admin:Password"]).Returns(ValidPassword);
        _configurationMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns(JwtExpirationMinutes.ToString());
        
        _handler = new LoginHandler(_jwtTokenServiceMock.Object, _configurationMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = ValidPassword
        };
        
        _jwtTokenServiceMock
            .Setup(s => s.GenerateToken(ValidUsername, "Admin"))
            .Returns(ValidToken);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(ValidToken);
        result.Username.Should().Be(ValidUsername);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(JwtExpirationMinutes + 1));
        
        _jwtTokenServiceMock.Verify(s => s.GenerateToken(ValidUsername, "Admin"), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenValidCredentials_UsernameCaseInsensitive_ShouldReturnLoginResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername.ToUpper(),
            Password = ValidPassword
        };
        
        _jwtTokenServiceMock
            .Setup(s => s.GenerateToken(ValidUsername.ToUpper(), "Admin"))
            .Returns(ValidToken);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(ValidToken);
        result.Username.Should().Be(ValidUsername.ToUpper());
    }

    [Test]
    public void HandleAsync_WhenInvalidUsername_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "wronguser",
            Password = ValidPassword
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(request);
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Test]
    public void HandleAsync_WhenInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = "wrongpassword"
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(request);
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Test]
    public void HandleAsync_WhenNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _handler.HandleAsync(null!);
        act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Test]
    public void HandleAsync_WhenNullUsername_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = null!,
            Password = ValidPassword
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(request);
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Test]
    public void HandleAsync_WhenEmptyUsername_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = string.Empty,
            Password = ValidPassword
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(request);
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Test]
    public void HandleAsync_WhenNullPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = null!
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(request);
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Test]
    public void HandleAsync_WhenEmptyPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = string.Empty
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(request);
        act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Test]
    public async Task HandleAsync_WhenValidCredentials_ShouldSetCorrectExpiration()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = ValidUsername,
            Password = ValidPassword
        };
        
        var expectedExpirationMinutes = 120;
        _configurationMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns(expectedExpirationMinutes.ToString());
        
        _jwtTokenServiceMock
            .Setup(s => s.GenerateToken(ValidUsername, "Admin"))
            .Returns(ValidToken);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(expectedExpirationMinutes - 1));
        result.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(expectedExpirationMinutes + 1));
    }
}
