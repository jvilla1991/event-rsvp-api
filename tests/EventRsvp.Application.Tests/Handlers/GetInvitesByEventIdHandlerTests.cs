using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class GetInvitesByEventIdHandlerTests
{
    private Mock<IInviteRepository> _inviteRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private GetInvitesByEventIdHandler _handler = null!;

    private const int TestEventId = 1;
    private const int NonExistentEventId = 999;

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

        _handler = new GetInvitesByEventIdHandler(_inviteRepositoryMock.Object, _eventRepositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenEventHasInvites_ShouldReturnAllInvites()
    {
        // Arrange
        var invites = new List<Invite>
        {
            new() { Id = 1, EventId = TestEventId, Name = "Alice", Token = "token1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, EventId = TestEventId, Name = "Bob", Token = "token2", CreatedAt = DateTime.UtcNow }
        };

        _inviteRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invites);

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(i => i.Name).Should().BeEquivalentTo(new[] { "Alice", "Bob" });
    }

    [Test]
    public async Task HandleAsync_WhenEventHasNoInvites_ShouldReturnEmptyCollection()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invite>());

        // Act
        var result = await _handler.HandleAsync(TestEventId);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidInviteException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentEventId);

        // Assert
        await act.Should().ThrowAsync<InvalidInviteException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldNotQueryInviteRepository()
    {
        // Act
        try { await _handler.HandleAsync(NonExistentEventId); } catch { }

        // Assert
        _inviteRepositoryMock.Verify(
            r => r.GetByEventIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var viewedAt = DateTime.UtcNow.AddMinutes(-5);
        var invites = new List<Invite>
        {
            new()
            {
                Id = 42,
                EventId = TestEventId,
                Name = "Charlie",
                Token = "abc123",
                Status = Domain.Enums.InviteStatus.Opened,
                ViewedAt = viewedAt,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _inviteRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invites);

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).Single();

        // Assert
        result.Id.Should().Be(42);
        result.EventId.Should().Be(TestEventId);
        result.Name.Should().Be("Charlie");
        result.Token.Should().Be("abc123");
        result.ViewedAt.Should().Be(viewedAt);
        result.Status.Should().Be("Opened");
    }
}
