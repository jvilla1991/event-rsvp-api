using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Enums;
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

    private static Invite BuildInvite(string token, DateTime? viewedAt = null, InviteStatus status = InviteStatus.NotOpened) => new()
    {
        Id = 1,
        EventId = 5,
        Name = "Bob",
        Token = token,
        Status = status,
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
        result.Status.Should().Be("Opened");
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
    public async Task HandleAsync_WhenAlreadyOpened_ShouldNotUpdate()
    {
        // Arrange — invite already has Opened status
        var originalViewedAt = DateTime.UtcNow.AddHours(-2);
        var invite = BuildInvite(ValidToken, viewedAt: originalViewedAt, status: InviteStatus.Opened);

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
    public async Task HandleAsync_WhenAlreadyAccepted_ShouldNotDowngradeToOpened()
    {
        // Arrange — someone already RSVP'd yes; reopening the link must not change status
        var invite = BuildInvite(ValidToken, viewedAt: DateTime.UtcNow.AddDays(-1), status: InviteStatus.Accepted);

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert
        result.Status.Should().Be("Accepted");
        _inviteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenAlreadyDeclined_ShouldNotDowngradeToOpened()
    {
        // Arrange — someone already RSVP'd no; reopening the link must not change status
        var invite = BuildInvite(ValidToken, viewedAt: DateTime.UtcNow.AddDays(-1), status: InviteStatus.Declined);

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert
        result.Status.Should().Be("Declined");
        _inviteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenNotOpenedButViewedAtAlreadySet_ShouldAdvanceStatusWithoutOverwritingViewedAt()
    {
        // Arrange — legacy invite: ViewedAt was stamped before the Status column existed,
        //           so Status is still NotOpened even though it was previously viewed.
        var originalViewedAt = DateTime.UtcNow.AddDays(-5);
        var invite = BuildInvite(ValidToken, viewedAt: originalViewedAt, status: InviteStatus.NotOpened);

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _inviteRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite i, CancellationToken _) => i);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert — status advances but original timestamp is preserved
        result.Status.Should().Be("Opened");
        result.ViewedAt.Should().Be(originalViewedAt);
        _inviteRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ShouldReturnNameFromInvite()
    {
        // Arrange — verifies the name is always passed through for form pre-fill
        var invite = BuildInvite(ValidToken);
        invite.Name = "Sarah Connor";

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _inviteRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite i, CancellationToken _) => i);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert
        result.Name.Should().Be("Sarah Connor");
    }

    [Test]
    public async Task HandleAsync_WhenInviteIsAnonymous_ShouldReturnEmptyName()
    {
        // Arrange — invite was created without a name
        var invite = BuildInvite(ValidToken);
        invite.Name = string.Empty;

        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(ValidToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _inviteRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite i, CancellationToken _) => i);

        // Act
        var result = await _handler.HandleAsync(ValidToken);

        // Assert
        result.Name.Should().BeEmpty();
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
