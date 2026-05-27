using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class GetPollResultsHandlerTests
{
    private Mock<IPollRepository> _pollRepositoryMock = null!;
    private Mock<IPollVoteRepository> _pollVoteRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private GetPollResultsHandler _handler = null!;
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

        _handler = new GetPollResultsHandler(
            _pollRepositoryMock.Object,
            _pollVoteRepositoryMock.Object,
            _eventRepositoryMock.Object);
    }

    private static Poll BuildPoll(int? eventId = null) => new()
    {
        Id = TestPollId,
        EventId = eventId ?? TestEventId,
        Question = "Best option?",
        Options = new List<string> { "Option A", "Option B", "Option C" }
    };

    [Test]
    public async Task HandleAsync_WhenPollHasVotes_ShouldReturnCorrectTotalAndCounts()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPoll());

        _pollVoteRepositoryMock
            .Setup(r => r.GetVoteCountByPollIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _pollVoteRepositoryMock
            .Setup(r => r.GetVoteCountsByOptionIndexAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { { 0, 3 }, { 1, 2 } });

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        result.PollId.Should().Be(TestPollId);
        result.TotalVotes.Should().Be(5);
        result.OptionVotes["0"].Should().Be(3);
        result.OptionVotes["1"].Should().Be(2);
        result.OptionVotes["2"].Should().Be(0); // option with no votes defaults to 0
    }

    [Test]
    public async Task HandleAsync_WhenPollHasNoVotes_ShouldReturnZerosForAllOptions()
    {
        // Arrange
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPoll());

        _pollVoteRepositoryMock
            .Setup(r => r.GetVoteCountByPollIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _pollVoteRepositoryMock
            .Setup(r => r.GetVoteCountsByOptionIndexAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        result.TotalVotes.Should().Be(0);
        result.OptionVotes.Should().HaveCount(3); // one entry per option
        result.OptionVotes.Values.Should().AllSatisfy(count => count.Should().Be(0));
    }

    [Test]
    public async Task HandleAsync_WhenPollHasVotes_ShouldIncludeAllOptionsEvenUnvoted()
    {
        // Arrange — only index 0 received votes; indices 1 and 2 should still appear with 0
        _pollRepositoryMock
            .Setup(r => r.GetByIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPoll());

        _pollVoteRepositoryMock
            .Setup(r => r.GetVoteCountByPollIdAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _pollVoteRepositoryMock
            .Setup(r => r.GetVoteCountsByOptionIndexAsync(TestPollId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { { 0, 2 } });

        // Act
        var result = await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        result.OptionVotes.Should().ContainKeys("0", "1", "2");
        result.OptionVotes["1"].Should().Be(0);
        result.OptionVotes["2"].Should().Be(0);
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidPollException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentId, TestPollId);

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

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, TestPollId);

        // Assert
        await act.Should().ThrowAsync<InvalidPollException>()
            .WithMessage("*Poll*not found*");
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
}
