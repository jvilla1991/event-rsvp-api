using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class ViewInviteHandlerTests
{
    private Mock<IInviteRepository> _inviteRepositoryMock = null!;
    private ViewInviteHandler _handler = null!;

    private const string ValidToken = "abc123validtoken";
    private const string NonExistentToken = "doesnotexist";

    [SetUp]
    public void SetUp()
    {
        _inviteRepositoryMock = new Mock<IInviteRepository>();
        _handler = new ViewInviteHandler(_inviteRepositoryMock.Object);
    }

    private static Invite BuildInvite(string token, DateTime? viewedAt = null) => new()
    {
        Id = 1,
        EventId = 5,
        Name = "Bob",
        Token = token,
        ViewedAt = viewedAt,
        CreatedAt = DateTime.UtcNow.AddDays(-1)
    };

    [Test]
    public async Task HandleAsync_WhenInviteNotYetViewed_ShouldSetViewedAt()
    {
        // Arrange
        var invite = BuildInvite(ValidToken, viewedAt: null);

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _inviteRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite i, CancellationToken _) => i);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert
        result.ViewedAt.Should().NotBeNull();
        result.IsViewed.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_WhenInviteNotYetViewed_ShouldCallUpdate()
    {
        // Arrange
        var invite = BuildInvite(ValidToken, viewedAt: null);

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _inviteRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite i, CancellationToken _) => i);

        // Act
        await _handler.HandleAsync(ValidToken);

        // Assert
        _inviteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenAlreadyViewed_ShouldNotUpdateViewedAt()
    {
        // Arrange
        var originalViewedAt = DateTime.UtcNow.AddHours(-2);
        var invite = BuildInvite(ValidToken, viewedAt: originalViewedAt);

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert
        result.ViewedAt.Should().Be(originalViewedAt);
        _inviteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenTokenNotFound_ShouldThrowInvalidInviteException()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(NonExistentToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        // Act
        var act = async () => await _handler.HandleAsync(NonExistentToken);

        // Assert
        await act.Should().ThrowAsync<InvalidInviteException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenTokenIsEmpty_ShouldThrowInvalidInviteException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Test]
    public async Task HandleAsync_ShouldReturnCorrectInviteDetails()
    {
        // Arrange
        var invite = BuildInvite(ValidToken);
        invite.Name = "Alice";
        invite.EventId = 7;

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _inviteRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite i, CancellationToken _) => i);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert
        result.Name.Should().Be("Alice");
        result.EventId.Should().Be(7);
        result.Token.Should().Be(ValidToken);
    }
}
