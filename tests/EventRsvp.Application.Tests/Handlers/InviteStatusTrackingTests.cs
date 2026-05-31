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

/// <summary>
/// Covers invite-status transitions triggered by the CreateRsvpHandler:
///   NotOpened / Opened  →  Accepted  (WillAttend = true)
///   NotOpened / Opened  →  Declined  (WillAttend = false)
///
/// Edge cases: token-based lookup, name-based lookup, anonymous invites,
/// upsert answer-change, and no matching invite.
/// </summary>
[TestFixture]
public class InviteStatusTrackingTests
{
    private Mock<IRsvpRepository> _rsvpRepo = null!;
    private Mock<IEventRepository> _eventRepo = null!;
    private Mock<IInviteRepository> _inviteRepo = null!;
    private Mock<IEmailService> _emailService = null!;
    private CreateRsvpHandler _handler = null!;

    private const int EventId = 1;
    private const string InviteToken = "tok-abc-123";

    [SetUp]
    public void SetUp()
    {
        _rsvpRepo = new Mock<IRsvpRepository>();
        _eventRepo = new Mock<IEventRepository>();
        _inviteRepo = new Mock<IInviteRepository>();
        _emailService = new Mock<IEmailService>();

        _eventRepo
            .Setup(r => r.GetByIdAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = EventId, Title = "Summer BBQ" });

        // Default: no existing RSVP (create path)
        _rsvpRepo
            .Setup(r => r.GetByEventIdAndNameAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rsvp?)null);

        // Default: AddAsync echoes back the rsvp
        _rsvpRepo
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rsvp r, CancellationToken _) => r);

        // Default: no invite found by token or name
        _inviteRepo
            .Setup(r => r.GetByTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        _handler = new CreateRsvpHandler(
            _rsvpRepo.Object,
            _eventRepo.Object,
            _inviteRepo.Object,
            _emailService.Object);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Invite BuildInvite(string name, InviteStatus status = InviteStatus.Opened) => new()
    {
        Id = 10,
        EventId = EventId,
        Name = name,
        Token = InviteToken,
        Status = status,
        CreatedAt = DateTime.UtcNow.AddDays(-1)
    };

    private static CreateRsvpRequest Accepting(string name, string? token = null) =>
        new() { Name = name, WillAttend = true, InviteToken = token };

    private static CreateRsvpRequest Declining(string name, string? token = null) =>
        new() { Name = name, WillAttend = false, InviteToken = token };

    // ── Status → Accepted ─────────────────────────────────────────────────────

    [Test]
    public async Task WhenRsvpAcceptsAndInviteFoundByName_ShouldSetInviteToAccepted()
    {
        // Arrange
        var invite = BuildInvite("Alice");
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        await _handler.HandleAsync(EventId, Accepting("Alice"));

        // Assert
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Accepted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WhenRsvpAcceptsAndInviteFoundByToken_ShouldSetInviteToAccepted()
    {
        // Arrange — token supplied; name does not need to match
        var invite = BuildInvite("Bob");
        _inviteRepo
            .Setup(r => r.GetByTokenAsync(InviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        await _handler.HandleAsync(EventId, Accepting("Bob", token: InviteToken));

        // Assert
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Accepted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WhenRsvpAcceptsViaToken_ShouldPreferTokenOverNameLookup()
    {
        // Arrange — token matches a different invite than the name would
        var inviteByToken = BuildInvite("Carol");
        var inviteByName = BuildInvite("Dave");

        _inviteRepo
            .Setup(r => r.GetByTokenAsync(InviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviteByToken);
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Carol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviteByName);

        // Act
        await _handler.HandleAsync(EventId, Accepting("Carol", token: InviteToken));

        // Assert — only the token-matched invite is updated, not the name-matched one
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Id == inviteByToken.Id && i.Status == InviteStatus.Accepted),
            It.IsAny<CancellationToken>()), Times.Once);
        _inviteRepo.Verify(r => r.GetByEventIdAndNameAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Status → Declined ─────────────────────────────────────────────────────

    [Test]
    public async Task WhenRsvpDeclinesAndInviteFoundByName_ShouldSetInviteToDeclined()
    {
        // Arrange
        var invite = BuildInvite("Eve");
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Eve", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        await _handler.HandleAsync(EventId, Declining("Eve"));

        // Assert
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Declined),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WhenRsvpDeclinesViaToken_ShouldSetInviteToDeclined()
    {
        // Arrange
        var invite = BuildInvite("Frank");
        _inviteRepo
            .Setup(r => r.GetByTokenAsync(InviteToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        await _handler.HandleAsync(EventId, Declining("Frank", token: InviteToken));

        // Assert
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Declined),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Name matching ─────────────────────────────────────────────────────────

    [Test]
    public async Task WhenRsvpNameMatchesInviteWithDifferentCase_ShouldStillUpdateInviteStatus()
    {
        // Arrange — invite stored as "Grace", RSVP submitted as "grace"
        var invite = BuildInvite("Grace");
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "grace", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);  // repository does case-insensitive match

        // Act
        await _handler.HandleAsync(EventId, Accepting("grace"));

        // Assert
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Accepted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WhenNoMatchingInviteExists_ShouldNotCallInviteUpdate()
    {
        // Arrange — no invite set up; defaults return null

        // Act
        await _handler.HandleAsync(EventId, Accepting("Heidi"));

        // Assert — invite UpdateAsync is never called
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task WhenInviteIsAnonymous_ShouldNotBeMatchedByNamedRsvp()
    {
        // Arrange — anonymous invite has empty name; a named RSVP should not match it
        var anonymousInvite = BuildInvite(string.Empty);
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anonymousInvite);

        // "Ivan" submits an RSVP; the repo should be asked for "ivan" (not "")
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "ivan", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite?)null);

        // Act
        await _handler.HandleAsync(EventId, Accepting("Ivan"));

        // Assert — anonymous invite is untouched
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Upsert (answer change) ────────────────────────────────────────────────

    [Test]
    public async Task WhenExistingRsvpUpdatedToAccept_ShouldAlsoUpdateInviteToAccepted()
    {
        // Arrange — person previously declined and is now changing their mind
        var existingRsvp = new Rsvp
        {
            Id = 99, EventId = EventId, Name = "Jack", WillAttend = false, CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _rsvpRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Jack", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRsvp);
        _rsvpRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rsvp r, CancellationToken _) => r);

        var invite = BuildInvite("Jack", InviteStatus.Declined);
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Jack", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        await _handler.HandleAsync(EventId, Accepting("Jack"));

        // Assert — invite updated to Accepted after answer change
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Accepted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WhenExistingRsvpUpdatedToDecline_ShouldAlsoUpdateInviteToDeclined()
    {
        // Arrange — person previously accepted and is now declining
        var existingRsvp = new Rsvp
        {
            Id = 88, EventId = EventId, Name = "Karen", WillAttend = true, CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _rsvpRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Karen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRsvp);
        _rsvpRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rsvp r, CancellationToken _) => r);

        var invite = BuildInvite("Karen", InviteStatus.Accepted);
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Karen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        await _handler.HandleAsync(EventId, Declining("Karen"));

        // Assert
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Declined),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Not-Opened invite can still be accepted ───────────────────────────────

    [Test]
    public async Task WhenInviteIsNotOpenedAndRsvpSubmitted_ShouldSetToAccepted()
    {
        // Arrange — person skipped opening the invite link and went straight to RSVP
        var invite = BuildInvite("Leo", InviteStatus.NotOpened);
        _inviteRepo
            .Setup(r => r.GetByEventIdAndNameAsync(EventId, "Leo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        await _handler.HandleAsync(EventId, Accepting("Leo"));

        // Assert — goes straight from NotOpened to Accepted (skips Opened)
        _inviteRepo.Verify(r => r.UpdateAsync(
            It.Is<Invite>(i => i.Status == InviteStatus.Accepted),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
