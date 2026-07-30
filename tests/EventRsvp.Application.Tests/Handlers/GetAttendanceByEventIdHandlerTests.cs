using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class GetAttendanceByEventIdHandlerTests
{
    private Mock<IInviteRepository> _inviteRepositoryMock = null!;
    private Mock<IRsvpRepository> _rsvpRepositoryMock = null!;
    private GetAttendanceByEventIdHandler _handler = null!;

    private const int TestEventId = 1;

    [SetUp]
    public void SetUp()
    {
        _inviteRepositoryMock = new Mock<IInviteRepository>();
        _rsvpRepositoryMock = new Mock<IRsvpRepository>();

        // Default: no invites and no RSVPs
        _inviteRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invite>());
        _rsvpRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rsvp>());

        _handler = new GetAttendanceByEventIdHandler(_inviteRepositoryMock.Object, _rsvpRepositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenInviteeHasRespondedWithWeddingFields_ShouldMapFieldsOntoInviteRow()
    {
        // Arrange — Alice was invited and responded with the wedding fields filled in
        var invites = new List<Invite>
        {
            new() { Id = 10, EventId = TestEventId, Name = "Alice", Token = "token1", Status = InviteStatus.Accepted, CreatedAt = DateTime.UtcNow }
        };
        var rsvps = new List<Rsvp>
        {
            new()
            {
                Id = 1,
                EventId = TestEventId,
                Name = "Alice",
                Status = RsvpStatus.Yes,
                Email = "alice@example.com",
                GuestCount = 2,
                MealChoice = "Fish",
                Note = "Gluten free",
                CreatedAt = DateTime.UtcNow
            }
        };

        _inviteRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invites);
        _rsvpRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvps);

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).ToList();

        // Assert — the invite-matched row carries the wedding fields from the RSVP
        result.Should().HaveCount(1);
        var row = result.Single();
        row.Source.Should().Be("invite");
        row.Email.Should().Be("alice@example.com");
        row.GuestCount.Should().Be(2);
        row.MealChoice.Should().Be("Fish");
        row.Note.Should().Be("Gluten free");
    }

    [Test]
    public async Task HandleAsync_WhenWalkInRsvpHasWeddingFields_ShouldMapFieldsOntoRsvpRow()
    {
        // Arrange — Bob was never invited but RSVP'd directly (walk-in)
        var rsvps = new List<Rsvp>
        {
            new()
            {
                Id = 2,
                EventId = TestEventId,
                Name = "Bob",
                Status = RsvpStatus.Yes,
                Email = "bob@example.com",
                GuestCount = 3,
                MealChoice = "Beef",
                Note = "Plus kids",
                CreatedAt = DateTime.UtcNow
            }
        };

        _rsvpRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvps);

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).ToList();

        // Assert — the walk-in row carries the wedding fields too
        result.Should().HaveCount(1);
        var row = result.Single();
        row.Source.Should().Be("rsvp");
        row.Email.Should().Be("bob@example.com");
        row.GuestCount.Should().Be(3);
        row.MealChoice.Should().Be("Beef");
        row.Note.Should().Be("Plus kids");
    }

    [Test]
    public async Task HandleAsync_WhenInviteeHasNotResponded_ShouldLeaveWeddingFieldsNull()
    {
        // Arrange — invite exists but no RSVP yet
        var invites = new List<Invite>
        {
            new() { Id = 11, EventId = TestEventId, Name = "Carol", Token = "token2", Status = InviteStatus.NotOpened, CreatedAt = DateTime.UtcNow }
        };

        _inviteRepositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invites);

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var row = result.Single();
        row.Email.Should().BeNull();
        row.GuestCount.Should().BeNull();
        row.MealChoice.Should().BeNull();
        row.Note.Should().BeNull();
    }
}
