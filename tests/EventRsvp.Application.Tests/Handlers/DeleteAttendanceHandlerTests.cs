using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class DeleteAttendanceHandlerTests
{
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private Mock<IInviteRepository> _inviteRepositoryMock = null!;
    private Mock<IRsvpRepository> _rsvpRepositoryMock = null!;
    private DeleteAttendanceHandler _handler = null!;

    private const int TestEventId = 1;
    private const int OtherEventId = 2;
    private const int NonExistentEventId = 999;
    private const int TestInviteId = 10;
    private const int TestRsvpId = 20;

    [SetUp]
    public void SetUp()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _inviteRepositoryMock = new Mock<IInviteRepository>();
        _rsvpRepositoryMock = new Mock<IRsvpRepository>();

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = TestEventId, Title = "Test Event" });

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        _handler = new DeleteAttendanceHandler(
            _eventRepositoryMock.Object,
            _inviteRepositoryMock.Object,
            _rsvpRepositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidRsvpException()
    {
        var act = async () => await _handler.HandleAsync(NonExistentEventId, "invite", TestInviteId);

        await act.Should().ThrowAsync<InvalidRsvpException>().WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenSourceUnknown_ShouldThrowInvalidRsvpException()
    {
        var act = async () => await _handler.HandleAsync(TestEventId, "bogus", TestInviteId);

        await act.Should().ThrowAsync<InvalidRsvpException>().WithMessage("*Unknown attendance source*");
    }

    // ── invite source ─────────────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_WhenInviteExists_ShouldDeleteInviteAndReturnTrue()
    {
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invite { Id = TestInviteId, EventId = TestEventId, Name = "Bob", Token = "t1" });
        _rsvpRepositoryMock
            .Setup(r => r.GetByEventIdAndNameAsync(TestEventId, "Bob", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rsvp?)null);
        _inviteRepositoryMock
            .Setup(r => r.DeleteAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(TestEventId, "invite", TestInviteId);

        result.Should().BeTrue();
        _inviteRepositoryMock.Verify(r => r.DeleteAsync(TestInviteId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenInvitedPersonAlsoRsvped_ShouldDeleteTheMatchingRsvp()
    {
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invite { Id = TestInviteId, EventId = TestEventId, Name = "Bob", Token = "t1" });
        _rsvpRepositoryMock
            .Setup(r => r.GetByEventIdAndNameAsync(TestEventId, "Bob", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = TestRsvpId, EventId = TestEventId, Name = "Bob" });
        _inviteRepositoryMock
            .Setup(r => r.DeleteAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.HandleAsync(TestEventId, "invite", TestInviteId);

        _rsvpRepositoryMock.Verify(r => r.DeleteAsync(TestRsvpId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenInviteDoesNotExist_ShouldReturnFalse()
    {
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        var result = await _handler.HandleAsync(TestEventId, "invite", TestInviteId);

        result.Should().BeFalse();
        _inviteRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenInviteBelongsToDifferentEvent_ShouldThrowAndNotDelete()
    {
        _inviteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestInviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invite { Id = TestInviteId, EventId = OtherEventId, Name = "Bob", Token = "t1" });

        var act = async () => await _handler.HandleAsync(TestEventId, "invite", TestInviteId);

        await act.Should().ThrowAsync<InvalidRsvpException>();
        _inviteRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── rsvp source ───────────────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_WhenRsvpExists_ShouldDeleteRsvpAndReturnTrue()
    {
        _rsvpRepositoryMock
            .Setup(r => r.GetByIdAsync(TestRsvpId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = TestRsvpId, EventId = TestEventId, Name = "Walk-in" });
        _rsvpRepositoryMock
            .Setup(r => r.DeleteAsync(TestRsvpId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(TestEventId, "rsvp", TestRsvpId);

        result.Should().BeTrue();
        _rsvpRepositoryMock.Verify(r => r.DeleteAsync(TestRsvpId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenRsvpDoesNotExist_ShouldReturnFalse()
    {
        _rsvpRepositoryMock
            .Setup(r => r.GetByIdAsync(TestRsvpId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rsvp?)null);

        var result = await _handler.HandleAsync(TestEventId, "rsvp", TestRsvpId);

        result.Should().BeFalse();
        _rsvpRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenRsvpBelongsToDifferentEvent_ShouldThrowAndNotDelete()
    {
        _rsvpRepositoryMock
            .Setup(r => r.GetByIdAsync(TestRsvpId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rsvp { Id = TestRsvpId, EventId = OtherEventId, Name = "Walk-in" });

        var act = async () => await _handler.HandleAsync(TestEventId, "rsvp", TestRsvpId);

        await act.Should().ThrowAsync<InvalidRsvpException>();
        _rsvpRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
