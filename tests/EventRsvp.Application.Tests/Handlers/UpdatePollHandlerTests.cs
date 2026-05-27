using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class UpdatePollHandlerTests
{
    private Mock<IPollRepository> _pollRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private UpdatePollHandler _handler = null!;
    private const int TestEventId = 1;
    private const int TestPollId = 10;
    private const int NonExistentId = 999;

    [SetUp]
    public void SetUp()
    {
        _pollRepositoryMock = new Mock<IPollRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = TestEventId, Title = "Test Event" });

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        _pollRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poll poll, CancellationToken _) => poll);

        _handler = new UpdatePollHandler(_pollRepositoryMock.Object, _eventRepositoryMock.Object);
    }

    private static Poll BuildExistingPoll(int? eventId = null) => new()
    {
        Id = TestPollId,
        EventId = eventId ?? TestEventId,
        Question = "Original question?",
        Options = new List<string> { "Original A", "Original B" },
        AllowMultiple = false,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static UpdatePollRequest ValidUpdateRequest() => new()
    {
        Question = "Updated question?",
        Options = new List<string> { "New Option A", "New Option B" },
        AllowMultiple = true
    };

    [Test]
    public async Task HandleAsync_WhenValidUpdate_ShouldReturnUpdatedPollResponse()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExistingPoll());
        var request = ValidUpdateRequest();

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Question.Should().Be("Updated question?");
        result.Options.Should().BeEquivalentTo(new[] { "New Option A", "New Option B" });
        result.AllowMultiple.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_WhenValidUpdate_ShouldPreserveCreatedAt()
    {
        // Arrange
        var originalCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existingPoll = BuildExistingPoll();
        existingPoll.CreatedAt = originalCreatedAt;

        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPoll);

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId, ValidUpdateRequest());

        // Assert
        result!.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Test]
    public async Task HandleAsync_WhenValidUpdate_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExistingPoll());

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId, ValidUpdateRequest());

        // Assert
        result!.UpdatedAt.Should().BeAfter(beforeUpdate);
    }

    [Test]
    public async Task HandleAsync_WhenQuestionHasWhitespace_ShouldTrimQuestion()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExistingPoll());
        var request = ValidUpdateRequest();
        request.Question = "  Updated question?  ";

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        _pollRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Poll>(p => p.Question == "Updated question?"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenOptionsHaveWhitespace_ShouldTrimOptions()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExistingPoll());
        var request = ValidUpdateRequest();
        request.Options = new List<string> { "  Option A  ", "  Option B  " };

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        _pollRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Poll>(p => p.Options[0] == "Option A" && p.Options[1] == "Option B"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenPollNotFound_ShouldReturnNull()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poll?)null);

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId, ValidUpdateRequest());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task HandleAsync_WhenPollNotFound_ShouldNotCallUpdate()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poll?)null);

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId, ValidUpdateRequest());

        // Assert
        _pollRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidPollException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentId, TestPollId, ValidUpdateRequest());

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
            .ReturnsAsync(BuildExistingPoll(eventId: 42));

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, ValidUpdateRequest());

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*does not belong*");
    }

    [Test]
    public async Task HandleAsync_WhenUpdatedQuestionIsEmpty_ShouldThrowInvalidPollException()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExistingPoll());
        var request = ValidUpdateRequest();
        request.Question = string.Empty;

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>();
    }

    [Test]
    public async Task HandleAsync_WhenUpdatedOptionsFewerThanTwo_ShouldThrowInvalidPollException()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildExistingPoll());
        var request = ValidUpdateRequest();
        request.Options = new List<string> { "Only one" };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>();
    }
}
