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
public class SubmitPollVoteHandlerTests
{
    private Mock<IPollRepository> _pollRepositoryMock = null!;
    private Mock<IPollVoteRepository> _pollVoteRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private SubmitPollVoteHandler _handler = null!;
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
            .Setup(r => r.AddAsync(It.IsAny<PollVote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PollVote vote, CancellationToken _) => vote);

        _handler = new SubmitPollVoteHandler(
            _pollRepositoryMock.Object,
            _pollVoteRepositoryMock.Object,
            _eventRepositoryMock.Object);
    }

    private static Poll BuildPoll(bool allowMultiple = false, int? eventId = null) => new()
    {
        Id = TestPollId,
        EventId = eventId ?? TestEventId,
        Question = "Pick one?",
        Options = new List<string> { "Option A", "Option B", "Option C" },
        AllowMultiple = allowMultiple
    };

    private void SetupPoll(Poll poll) =>
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poll);

    [Test]
    public async Task HandleAsync_WhenValidSingleChoiceVote_ShouldAddVote()
    {
        // Arrange
        SetupPoll(BuildPoll(allowMultiple: false));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 0 } };

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        _pollVoteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PollVote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenValidMultiChoiceVote_ShouldAddVote()
    {
        // Arrange
        SetupPoll(BuildPoll(allowMultiple: true));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 0, 2 } };

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        _pollVoteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PollVote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenSingleChoicePollAndMultipleOptionsSelected_ShouldThrowInvalidPollVoteException()
    {
        // Arrange
        SetupPoll(BuildPoll(allowMultiple: false));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 0, 1 } };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollVoteException>()
            .WithMessage("*Exactly one option*");
    }

    [Test]
    public async Task HandleAsync_WhenSingleChoicePollAndNoOptionsSelected_ShouldThrowInvalidPollVoteException()
    {
        // Arrange
        SetupPoll(BuildPoll(allowMultiple: false));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int>() };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollVoteException>();
    }

    [Test]
    public async Task HandleAsync_WhenMultiChoicePollAndNoOptionsSelected_ShouldThrowInvalidPollVoteException()
    {
        // Arrange
        SetupPoll(BuildPoll(allowMultiple: true));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int>() };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollVoteException>()
            .WithMessage("*At least one option*");
    }

    [Test]
    public async Task HandleAsync_WhenOptionIndexOutOfBounds_ShouldThrowInvalidPollVoteException()
    {
        // Arrange - poll has 3 options (indices 0-2), so index 5 is invalid
        SetupPoll(BuildPoll(allowMultiple: false));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 5 } };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollVoteException>()
            .WithMessage("*between 0 and*");
    }

    [Test]
    public async Task HandleAsync_WhenNegativeOptionIndex_ShouldThrowInvalidPollVoteException()
    {
        // Arrange
        SetupPoll(BuildPoll(allowMultiple: false));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { -1 } };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollVoteException>();
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidPollException()
    {
        // Arrange
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 0 } };

        // Act
        var act = async () => await _handler.HandleAsync(NonExistentId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*Event*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenPollNotFound_ShouldThrowInvalidPollException()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poll?)null);

        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 0 } };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*Poll*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenPollBelongsToDifferentEvent_ShouldThrowInvalidPollException()
    {
        // Arrange
        SetupPoll(BuildPoll(eventId: 42)); // poll belongs to event 42, not TestEventId
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 0 } };

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*does not belong*");
    }

    [Test]
    public async Task HandleAsync_WhenValid_ShouldNotAddVoteForWrongPoll()
    {
        // Arrange
        SetupPoll(BuildPoll(allowMultiple: false));
        var request = new SubmitVoteRequest { SelectedOptions = new List<int> { 0 } };

        // Act
        await _handler.HandleAsync(TestEventId, TestPollId, request);

        // Assert — the saved vote should reference the correct poll ID
        _pollVoteRepositoryMock.Verify(r => r.AddAsync(
            It.Is<PollVote>(v => v.PollId == TestPollId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
