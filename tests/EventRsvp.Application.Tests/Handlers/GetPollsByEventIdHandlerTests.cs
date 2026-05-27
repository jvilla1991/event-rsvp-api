using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class GetPollsByEventIdHandlerTests
{
    private Mock<IPollRepository> _pollRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private GetPollsByEventIdHandler _handler = null!;
    private const int TestEventId = 1;
    private const int NonExistentEventId = 999;

    [SetUp]
    public void SetUp()
    {
        _pollRepositoryMock = new Mock<IPollRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = TestEventId, Title = "Test Event" });

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        _handler = new GetPollsByEventIdHandler(_pollRepositoryMock.Object, _eventRepositoryMock.Object);
    }

    private static Poll BuildPoll(int id, int eventId = TestEventId) => new()
    {
        Id = id,
        EventId = eventId,
        Question = $"Question {id}?",
        Options = new List<string> { "Yes", "No" },
        AllowMultiple = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Test]
    public async Task HandleAsync_WhenEventHasPolls_ShouldReturnAllPolls()
    {
        // Arrange
        var polls = new List<Poll> { BuildPoll(1), BuildPoll(2), BuildPoll(3) };

        _pollRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(polls);

        // Act
        var result = await _handler.HandleAsync(TestEventId);

        // Assert
        result.Should().HaveCount(3);
        result.Select(p => p.Id).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Test]
    public async Task HandleAsync_WhenEventHasPolls_ShouldMapAllFields()
    {
        // Arrange
        var poll = new Poll
        {
            Id = 1,
            EventId = TestEventId,
            Question = "What is your preference?",
            Options = new List<string> { "Option A", "Option B", "Option C" },
            AllowMultiple = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        _pollRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Poll> { poll });

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).Single();

        // Assert
        result.Id.Should().Be(1);
        result.EventId.Should().Be(TestEventId);
        result.Question.Should().Be("What is your preference?");
        result.Options.Should().BeEquivalentTo(new[] { "Option A", "Option B", "Option C" });
        result.AllowMultiple.Should().BeTrue();
        result.CreatedAt.Should().Be(poll.CreatedAt);
        result.UpdatedAt.Should().Be(poll.UpdatedAt);
    }

    [Test]
    public async Task HandleAsync_WhenEventHasNoPolls_ShouldReturnEmptyCollection()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Poll>());

        // Act
        var result = await _handler.HandleAsync(TestEventId);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidPollException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentEventId);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldNotQueryPollRepository()
    {
        // Act
        try { await _handler.HandleAsync(NonExistentEventId); } catch { }

        // Assert
        _pollRepositoryMock.Verify(r => r.GetByEventIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
