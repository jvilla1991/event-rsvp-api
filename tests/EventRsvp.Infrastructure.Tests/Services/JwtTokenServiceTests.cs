using EventRsvp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EventRsvp.Infrastructure.Tests.Services;

[TestFixture]
public class JwtTokenServiceTests
{
    private Mock<IOptions<JwtSettings>> _mockOptions = null!;
    private JwtTokenService _service = null!;
    private JwtSettings _jwtSettings = null!;

    [SetUp]
    public void SetUp()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = new string('A', 32) + "TestSecretKeyForJwtTokenServiceTests",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };

        _mockOptions = new Mock<IOptions<JwtSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_jwtSettings);

        _service = new JwtTokenService(_mockOptions.Object);
    }

    [Test]
    public void GenerateToken_WhenValidInputs_ShouldReturnToken()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().NotContain(" ");
        token.Should().NotContain("\t");
        token.Should().NotContain("\n");
    }

    [Test]
    public void GenerateToken_WhenValidInputs_ShouldReturnValidJwtFormat()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);

        // Assert
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT tokens should have 3 parts separated by dots");
    }

    [Test]
    public void GenerateToken_ShouldIncludeUsernameInClaims()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
    }

    [Test]
    public void GenerateToken_ShouldIncludeRoleInClaims()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
    }

    [Test]
    public void GenerateToken_ShouldIncludeJtiClaim()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Test]
    public void GenerateToken_ShouldIncludeIatClaim()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Test]
    public void GenerateToken_ShouldSetCorrectIssuer()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.Issuer.Should().Be(_jwtSettings.Issuer);
    }

    [Test]
    public void GenerateToken_ShouldSetCorrectAudience()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    [Test]
    public void GenerateToken_ShouldSetExpiration()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token = _service.GenerateToken(username, role);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jsonToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes + 1));
    }

    [Test]
    public void ValidateToken_WhenValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";
        var token = _service.GenerateToken(username, role);

        // Act
        var principal = _service.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Test]
    public void ValidateToken_WhenValidToken_ShouldContainCorrectClaims()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";
        var token = _service.GenerateToken(username, role);

        // Act
        var principal = _service.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role);
    }

    [Test]
    public void ValidateToken_WhenInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _service.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public void ValidateToken_WhenEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var emptyToken = string.Empty;

        // Act
        var principal = _service.ValidateToken(emptyToken);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public void ValidateToken_WhenTokenWithWrongSecret_ShouldReturnNull()
    {
        // Arrange
        var otherSettings = new JwtSettings
        {
            SecretKey = new string('B', 32) + "DifferentSecretKeyForTesting",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };
        var otherOptions = Mock.Of<IOptions<JwtSettings>>(x => x.Value == otherSettings);
        var otherService = new JwtTokenService(otherOptions);
        var token = otherService.GenerateToken("testuser", "Admin");

        // Act - Validate with original service (different secret key)
        var principal = _service.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public void ValidateToken_WhenExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var expiredSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = -1 // Expired immediately
        };
        var expiredOptions = Mock.Of<IOptions<JwtSettings>>(x => x.Value == expiredSettings);
        var expiredService = new JwtTokenService(expiredOptions);
        var expiredToken = expiredService.GenerateToken("testuser", "Admin");

        // Wait a moment to ensure token is expired
        Thread.Sleep(100);

        // Act
        var principal = _service.ValidateToken(expiredToken);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public void ValidateToken_WhenTokenWithWrongIssuer_ShouldReturnNull()
    {
        // Arrange
        var otherSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = "DifferentIssuer",
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = 60
        };
        var otherOptions = Mock.Of<IOptions<JwtSettings>>(x => x.Value == otherSettings);
        var otherService = new JwtTokenService(otherOptions);
        var token = otherService.GenerateToken("testuser", "Admin");

        // Act
        var principal = _service.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public void ValidateToken_WhenTokenWithWrongAudience_ShouldReturnNull()
    {
        // Arrange
        var otherSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = "DifferentAudience",
            ExpirationMinutes = 60
        };
        var otherOptions = Mock.Of<IOptions<JwtSettings>>(x => x.Value == otherSettings);
        var otherService = new JwtTokenService(otherOptions);
        var token = otherService.GenerateToken("testuser", "Admin");

        // Act
        var principal = _service.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Test]
    public void GenerateToken_WithDifferentUsernames_ShouldGenerateDifferentTokens()
    {
        // Arrange
        const string role = "Admin";

        // Act
        var token1 = _service.GenerateToken("user1", role);
        var token2 = _service.GenerateToken("user2", role);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Test]
    public void GenerateToken_WithDifferentRoles_ShouldGenerateDifferentTokens()
    {
        // Arrange
        const string username = "testuser";

        // Act
        var token1 = _service.GenerateToken(username, "Admin");
        var token2 = _service.GenerateToken(username, "User");

        // Assert
        token1.Should().NotBe(token2);
    }

    [Test]
    public void GenerateToken_WithSameInputs_ShouldGenerateDifferentTokens()
    {
        // Arrange
        const string username = "testuser";
        const string role = "Admin";

        // Act
        var token1 = _service.GenerateToken(username, role);
        Thread.Sleep(10); // Small delay to ensure different JTI
        var token2 = _service.GenerateToken(username, role);

        // Assert
        token1.Should().NotBe(token2, "Each token should have a unique JTI claim");
    }
}
