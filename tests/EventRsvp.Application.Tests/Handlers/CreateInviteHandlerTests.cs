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
public class CreateInviteHandlerTests
{
    private Mock<IInviteRepository> _inviteRepositoryMock = null!;
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private CreateInviteHandler _handler = null!;

    private const int TestEventId = 1;
    private const int NonExistentEventId = 999;

    [SetUp]
    public void SetUp()
    {
        _inviteRepositoryMock = new Mock<IInviteRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = TestEventId, Title = "Test Event" });

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        _handler = new CreateInviteHandler(_inviteRepositoryMock.Object, _eventRepositoryMock.Object);
    }

    private static CreateInviteRequest NamedRequest(string name) => new() { Name = name };
    private static CreateInviteRequest AnonymousRequest() => new() { Name = null };

    private static Invite BuildSavedInvite(string name, int id = 1) => new()
    {
        Id = id,
        EventId = TestEventId,
        Name = name,
        Token = Guid.NewGuid().ToString("N"),
        CreatedAt = DateTime.UtcNow
    };

    [Test]
    public async Task HandleAsync_WhenNameProvided_ShouldReturnMappedInviteResponse()
    {
        // Arrange
        var request = NamedRequest("Alice");
        var saved = BuildSavedInvite("Alice");

        _inviteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        // Act
        var result = await _handler.HandleAsync(TestEventId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(saved.Id);
        result.EventId.Should().Be(TestEventId);
        result.Name.Should().Be("Alice");
        result.Token.Should().Be(saved.Token);
        result.ViewedAt.Should().BeNull();
        result.IsViewed.Should().BeFalse();
    }

    [Test]
    public async Task HandleAsync_WhenNameIsNull_ShouldSucceedWithEmptyName()
    {
        // Arrange — Name is optional; null becomes empty string
        var request = AnonymousRequest();
        var saved = BuildSavedInvite(string.Empty);

        _inviteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        // Act
        var act = async () => await _handler.HandleAsync(TestEventId, request);

        // Assert — should not throw
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task HandleAsync_WhenNameIsNull_ShouldStoreEmptyString()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite inv, CancellationToken _) => { inv.Id = 1; return inv; });

        // Act
        await _handler.HandleAsync(TestEventId, AnonymousRequest());

        // Assert
        _inviteRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Invite>(i => i.Name == string.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenNameHasWhitespace_ShouldTrimName()
    {
        // Arrange
        var request = NamedRequest("  Bob  ");

        _inviteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite inv, CancellationToken _) => { inv.Id = 1; return inv; });

        // Act
        await _handler.HandleAsync(TestEventId, request);

        // Assert
        _inviteRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Invite>(i => i.Name == "Bob"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenValidRequest_ShouldGenerateNonEmptyToken()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite inv, CancellationToken _) => { inv.Id = 1; return inv; });

        // Act
        var result = await _handler.HandleAsync(TestEventId, NamedRequest("Bob"));

        // Assert
        result.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldThrowInvalidInviteException()
    {
        // Act
        var act = async () => await _handler.HandleAsync(NonExistentEventId, NamedRequest("Bob"));

        // Assert
        await act.Should().ThrowAsync<InvalidInviteException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task HandleAsync_WhenEventNotFound_ShouldNotCallRepository()
    {
        // Act
        try { await _handler.HandleAsync(NonExistentEventId, NamedRequest("Bob")); } catch { }

        // Assert
        _inviteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenValid_ShouldSetCorrectEventId()
    {
        // Arrange
        _inviteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSavedInvite("Bob"));

        // Act
        await _handler.HandleAsync(TestEventId, NamedRequest("Bob"));

        // Assert
        _inviteRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Invite>(i => i.EventId == TestEventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
