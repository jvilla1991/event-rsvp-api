using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class DeletePollHandlerTests
{
    private Mock<IPollRepository> _pollRepositoryMock = null!;
    private Mock<IPollVoteRepository> _pollVoteRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private DeletePollHandler _handler = null!;
    private const int TestEventId = 1;
    private const int TestPollId = 10;
    private const int NonExistentId = 999;

    [SetUp]
    public void SetUp()
    {
        _pollRepositoryMock = new Mock<IPollRepository>();
        _pollVoteRepositoryMock = new Mock<IPollVoteRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = TestEventId, Title = "Test Event" });

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        _pollVoteRepositoryMock
            .Setup(r => r.DeleteByPollIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _pollRepositoryMock
            .Setup(r => r.DeleteAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new DeletePollHandler(
            _pollRepositoryMock.Object,
            _pollVoteRepositoryMock.Object,
            _eventRepositoryMock.Object);
    }

    private static Poll BuildPoll(int? eventId = null) => new()
    {
        Id = TestPollId,
        EventId = eventId ?? TestEventId,
        Question = "Delete me?",
        Options = new List<string> { "Yes", "No" }
    };

    [Test]
    public async Task HandleAsync_WhenPollExists_ShouldReturnTrue()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPoll());

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_WhenPollExists_ShouldDeleteVotesBeforePoll()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPoll());

        var callOrder = new List<string>();
        _pollVoteRepositoryMock
            .Setup(r => r.DeleteByPollIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() => callOrder.Add("votes"));
        _pollRepositoryMock
            .Setup(r => r.DeleteAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() => callOrder.Add("poll"));

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert — votes must be deleted before the poll itself
        callOrder.Should().Equal("votes", "poll");
    }

    [Test]
    public async Task HandleAsync_WhenPollNotFound_ShouldReturnFalse()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poll?)null);

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HandleAsync_WhenPollNotFound_ShouldNotDeleteAnything()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poll?)null);

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        _pollVoteRepositoryMock.Verify(r => r.DeleteByPollIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _pollRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidPollException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentId, TestPollId);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenPollBelongsToDifferentEvent_ShouldThrowInvalidPollException()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPoll(eventId: 42));

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*does not belong*");
    }

    [Test]
    public async Task HandleAsync_WhenPollBelongsToDifferentEvent_ShouldNotDelete()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPoll(eventId: 42));

        // Act
        try { await _handler.HandleAsync(TestEventId, TestPollId); } catch { }

        // Assert
        _pollVoteRepositoryMock.Verify(r => r.DeleteByPollIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _pollRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
