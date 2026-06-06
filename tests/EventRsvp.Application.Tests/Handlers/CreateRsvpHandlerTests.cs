using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Application.Services;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Enums;
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
            Status = "Yes"
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            Status = RsvpStatus.Yes,
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
        result.Status.Should().Be("Yes");
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
            Status = "Yes"
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            Status = RsvpStatus.Yes,
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
            Status = "Yes"
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(TestEventId, request);
        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task HandleAsync_WhenStatusIsInvalid_ShouldThrowException()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Perhaps"
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(TestEventId, request);
        await act.Should().ThrowAsync<Domain.Exceptions.InvalidRsvpException>()
            .WithMessage("*Invalid RSVP status*");
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowException()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Yes"
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
            Status = "Yes"
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            Status = RsvpStatus.Yes,
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
    public async Task HandleAsync_WhenStatusIsNo_ShouldSetStatusToNo()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "No"
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            Status = RsvpStatus.No,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.Status.Should().Be("No");
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.Status == RsvpStatus.No && rsvp.EventId == TestEventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenStatusIsMaybe_ShouldSetStatusToMaybe()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "maybe" // case-insensitive
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            Status = RsvpStatus.Maybe,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.Status.Should().Be("Maybe");
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.Status == RsvpStatus.Maybe && rsvp.EventId == TestEventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenMaybeWithProposedTime_ShouldKeepProposedTime()
    {
        // Arrange — Maybe responders are allowed to suggest an alternative time
        var proposedTime = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var request = new CreateRsvpRequest
        {
            Name = "Jane Doe",
            Status = "Maybe",
            ProposedTime = proposedTime
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = 1, EventId = TestEventId, Name = "Jane Doe", Status = RsvpStatus.Maybe, ProposedTime = proposedTime });

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.ProposedTime.Should().Be(proposedTime);
        _rsvpRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.ProposedTime == proposedTime),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenStatusNoAndProposedTimeProvided_ShouldMapProposedTime()
    {
        // Arrange
        var proposedTime = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var request = new CreateRsvpRequest
        {
            Name = "Jane Doe",
            Status = "No",
            ProposedTime = proposedTime
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "Jane Doe",
            Status = RsvpStatus.No,
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
    public async Task HandleAsync_WhenStatusYesAndProposedTimeProvided_ShouldIgnoreProposedTime()
    {
        // Arrange — ProposedTime should be discarded when the answer is a definite Yes
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            Status = "Yes",
            ProposedTime = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc)
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            Status = RsvpStatus.Yes,
            ProposedTime = null,
            CreatedAt = DateTime.UtcNow
        };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert — handler clears ProposedTime when the answer is Yes
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
        var request = new CreateRsvpRequest { Name = "Jane Doe", Status = "No", ProposedTime = proposedTime };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = 1, EventId = TestEventId, Name = "Jane Doe", Status = RsvpStatus.No, ProposedTime = proposedTime });

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
        var request = new CreateRsvpRequest { Name = "Jane Doe", Status = "No", ProposedTime = null };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = 1, EventId = TestEventId, Name = "Jane Doe", Status = RsvpStatus.No, ProposedTime = null });

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert — no email when there is nothing to propose
        _emailServiceMock.Verify(e => e.SendTimeProposalNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenStatusYes_ShouldNotSendEmailNotification()
    {
        // Arrange
        var request = new CreateRsvpRequest { Name = "John Doe", Status = "Yes" };

        _rsvpRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = 1, EventId = TestEventId, Name = "John Doe", Status = RsvpStatus.Yes, ProposedTime = null });

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _emailServiceMock.Verify(e => e.SendTimeProposalNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenStatusNoAndNoProposedTime_ShouldReturnNullProposedTime()
    {
        // Arrange — declining without proposing a time
        var request = new CreateRsvpRequest
        {
            Name = "Jane Doe",
            Status = "No",
            ProposedTime = null
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "Jane Doe",
            Status = RsvpStatus.No,
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
