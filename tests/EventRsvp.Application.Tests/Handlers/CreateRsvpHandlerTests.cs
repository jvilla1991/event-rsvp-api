using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Application.Services;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class CreateRsvpHandlerTests
{
    private Mock<IRsvpRepository> _rsvpRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private Mock<IInviteRepository> _inviteRepositoryMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private CreateRsvpHandler _handler = null!;
    private const int TestEventId = 1;

    [SetUp]
    public void SetUp()
    {
        _rsvpRepositoryMock = new Mock<IRsvpRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _inviteRepositoryMock = new Mock<IInviteRepository>();
        _emailServiceMock = new Mock<IEmailService>();

        // Default: no existing RSVP for this person
        _rsvpRepositoryMock
            .Setup(r => r.GetByEventIdAndNameAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Rsvp?)null);

        // Default: no matching invite
        _inviteRepositoryMock
            .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Invite?)null);
        _inviteRepositoryMock
            .Setup(r => r.GetByEventIdAndNameAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Invite?)null);

        // Setup default event to exist
        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = TestEventId, Title = "Test Event" });

        _handler = new CreateRsvpHandler(
            _rsvpRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _inviteRepositoryMock.Object,
            _emailServiceMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenValidRequest_ShouldReturnRsvpResponse()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            WillAttend = true
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            WillAttend = true,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("John Doe");
        result.WillAttend.Should().BeTrue();
        _rsvpRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventRepositoryMock.Verify(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenNameHasWhitespace_ShouldTrimName()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "  John Doe  ",
            WillAttend = true
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            WillAttend = true,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.Name == "John Doe" && rsvp.EventId == TestEventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenInvalidRsvp_ShouldThrowException()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = string.Empty,
            WillAttend = true
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(TestEventId, request);
        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowException()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            WillAttend = true
        };

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        var act = async () => await _handler.HandleAsync(999, request);
        await act.Should().ThrowAsync<Domain.Exceptions.InvalidRsvpException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_ShouldSetEventIdOnRsvp()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            WillAttend = true
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            WillAttend = true,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.EventId == TestEventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenWillAttendIsFalse_ShouldSetWillAttendToFalse()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            WillAttend = false
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            WillAttend = false,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.WillAttend.Should().BeFalse();
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.WillAttend == false && rsvp.EventId == TestEventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenWillAttendFalseAndProposedTimeProvided_ShouldMapProposedTime()
    {
        // Arrange
        var proposedTime = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var request = new CreateRsvpRequest
        {
            Name = "Jane Doe",
            WillAttend = false,
            ProposedTime = proposedTime
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "Jane Doe",
            WillAttend = false,
            ProposedTime = proposedTime,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.ProposedTime.Should().Be(proposedTime);
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.ProposedTime == proposedTime),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenWillAttendTrueAndProposedTimeProvided_ShouldIgnoreProposedTime()
    {
        // Arrange — ProposedTime should be discarded when WillAttend is true
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            WillAttend = true,
            ProposedTime = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc)
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            WillAttend = true,
            ProposedTime = null,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert — handler clears ProposedTime when WillAttend is true
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.ProposedTime == null),
            It.IsAny<CancellationToken>()), Times.Once);
        result.ProposedTime.Should().BeNull();
    }

    [Test]
    public async Task HandleAsync_WhenProposedTimeSet_ShouldSendEmailNotification()
    {
        // Arrange
        var proposedTime = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var request = new CreateRsvpRequest { Name = "Jane Doe", WillAttend = false, ProposedTime = proposedTime };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = 1, EventId = TestEventId, Name = "Jane Doe", WillAttend = false, ProposedTime = proposedTime });

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _emailServiceMock.Verify(e => e.SendTimeProposalNotificationAsync(
            "Jane Doe", "Test Event", proposedTime, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenNoProposedTime_ShouldNotSendEmailNotification()
    {
        // Arrange — plain decline with no proposed time
        var request = new CreateRsvpRequest { Name = "Jane Doe", WillAttend = false, ProposedTime = null };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = 1, EventId = TestEventId, Name = "Jane Doe", WillAttend = false, ProposedTime = null });

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert — no email when there is nothing to propose
        _emailServiceMock.Verify(e => e.SendTimeProposalNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenWillAttendTrue_ShouldNotSendEmailNotification()
    {
        // Arrange
        var request = new CreateRsvpRequest { Name = "John Doe", WillAttend = true };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = 1, EventId = TestEventId, Name = "John Doe", WillAttend = true, ProposedTime = null });

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _emailServiceMock.Verify(e => e.SendTimeProposalNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenWillAttendFalseAndNoProposedTime_ShouldReturnNullProposedTime()
    {
        // Arrange — declining without proposing a time
        var request = new CreateRsvpRequest
        {
            Name = "Jane Doe",
            WillAttend = false,
            ProposedTime = null
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "Jane Doe",
            WillAttend = false,
            ProposedTime = null,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.ProposedTime.Should().BeNull();
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.ProposedTime == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

