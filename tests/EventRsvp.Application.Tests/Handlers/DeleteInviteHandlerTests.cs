using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class DeleteInviteHandlerTests
{
    private Mock<IInviteRepository> _inviteRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private DeleteInviteHandler _handler = null!;

    private const int TestEventId = 1;
    private const int TestInviteId = 10;
    private const int NonExistentEventId = 999;
    private const int NonExistentInviteId = 888;

    [SetUp]
    public void SetUp()
    {
        _inviteRepositoryMock = new Mock<IInviteRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = TestEventId, Title = "Test Event" });

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        _handler = new DeleteInviteHandler(_inviteRepositoryMock.Object, _eventRepositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenInviteExists_ShouldReturnTrue()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invite { Id = TestInviteId, EventId = TestEventId, Name = "Bob", Token = "t1" });

        _inviteRepositoryMock
            .Setup(r => r.DeleteAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestInviteId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_WhenInviteExists_ShouldCallDeleteRepository()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invite { Id = TestInviteId, EventId = TestEventId, Name = "Bob", Token = "t1" });

        _inviteRepositoryMock
            .Setup(r => r.DeleteAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(TestEventId, TestInviteId);

        // Assert
        _inviteRepositoryMock.Verify(r => r.DeleteAsync(TestInviteId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenInviteDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        // Act
        var result = await _handler.HandleAsync(TestEventId, NonExistentInviteId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HandleAsync_WhenInviteDoesNotExist_ShouldNotCallDelete()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        // Act
        await _handler.HandleAsync(TestEventId, NonExistentInviteId);

        // Assert
        _inviteRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidInviteException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentEventId, TestInviteId);

        // Assert
        await act.Should().ThrowAsync<InvalidInviteException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenInviteBelongsToDifferentEvent_ShouldThrowInvalidInviteException()
    {
        // Arrange — invite belongs to event 2, not TestEventId (1)
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invite { Id = TestInviteId, EventId = 2, Name = "Bob", Token = "t1" });

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestInviteId);

        // Assert
        await act.Should().ThrowAsync<InvalidInviteException>();
    }

    [Test]
    public async Task HandleAsync_WhenInviteBelongsToDifferentEvent_ShouldNotCallDelete()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invite { Id = TestInviteId, EventId = 2, Name = "Bob", Token = "t1" });

        // Act
        try { await _handler.HandleAsync(TestEventId, TestInviteId); } catch { }

        // Assert
        _inviteRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
