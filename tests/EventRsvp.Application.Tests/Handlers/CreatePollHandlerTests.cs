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
public class CreatePollHandlerTests
{
    private Mock<IPollRepository> _pollRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private CreatePollHandler _handler = null!;
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

        _handler = new CreatePollHandler(_pollRepositoryMock.Object, _eventRepositoryMock.Object);
    }

    private static CreatePollRequest ValidRequest(bool allowMultiple = false) => new()
    {
        Question = "What time works best?",
        Options = new List<string> { "Morning", "Afternoon", "Evening" },
        AllowMultiple = allowMultiple
    };

    private static Poll BuildSavedPoll(CreatePollRequest request, int id = 1) => new()
    {
        Id = id,
        EventId = TestEventId,
        Question = request.Question,
        Options = request.Options,
        AllowMultiple = request.AllowMultiple,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Test]
    public async Task HandleAsync_WhenValidRequest_ShouldReturnMappedPollResponse()
    {
        // Arrange
        var request = ValidRequest();
        var savedPoll = BuildSavedPoll(request);

        _pollRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedPoll);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(savedPoll.Id);
        result.EventId.Should().Be(TestEventId);
        result.Question.Should().Be(request.Question);
        result.Options.Should().BeEquivalentTo(request.Options);
        result.AllowMultiple.Should().BeFalse();
    }

    [Test]
    public async Task HandleAsync_WhenAllowMultipleIsTrue_ShouldSetAllowMultiple()
    {
        // Arrange
        var request = ValidRequest(allowMultiple: true);
        var savedPoll = BuildSavedPoll(request);

        _pollRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedPoll);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.AllowMultiple.Should().BeTrue();
    }

    [Test]
    public async Task HandleAsync_WhenQuestionHasLeadingTrailingWhitespace_ShouldTrimQuestion()
    {
        // Arrange
        var request = ValidRequest();
        request.Question = "  What time works best?  ";

        _pollRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSavedPoll(request));

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _pollRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Poll>(p => p.Question == "What time works best?"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenOptionsHaveWhitespace_ShouldTrimOptions()
    {
        // Arrange
        var request = ValidRequest();
        request.Options = new List<string> { "  Morning  ", "  Afternoon  " };

        _pollRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSavedPoll(request));

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _pollRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Poll>(p => p.Options[0] == "Morning" && p.Options[1] == "Afternoon"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidPollException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentEventId, ValidRequest());

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldNotCallRepository()
    {
        // Act
        try { await _handler.HandleAsync(NonExistentEventId, ValidRequest()); } catch { }

        // Assert
        _pollRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenQuestionIsEmpty_ShouldThrowInvalidPollException()
    {
        // Arrange
        var request = ValidRequest();
        request.Question = string.Empty;

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>();
    }

    [Test]
    public async Task HandleAsync_WhenFewerThanTwoOptions_ShouldThrowInvalidPollException()
    {
        // Arrange
        var request = ValidRequest();
        request.Options = new List<string> { "Only one option" };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*at least 2 options*");
    }

    [Test]
    public async Task HandleAsync_WhenOptionIsEmpty_ShouldThrowInvalidPollException()
    {
        // Arrange
        var request = ValidRequest();
        request.Options = new List<string> { "Valid Option", "" };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>();
    }

    [Test]
    public async Task HandleAsync_WhenValid_ShouldSetEventIdOnPoll()
    {
        // Arrange
        var request = ValidRequest();

        _pollRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Poll>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSavedPoll(request));

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _pollRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Poll>(p => p.EventId == TestEventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
