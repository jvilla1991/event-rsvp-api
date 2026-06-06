using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class GetRsvpsByEventIdHandlerTests
{
    private Mock<IRsvpRepository> _repositoryMock = null!;
    private GetRsvpsByEventIdHandler _handler = null!;
    private const int TestEventId = 1;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IRsvpRepository>();
        _handler = new GetRsvpsByEventIdHandler(_repositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenRsvpsExist_ShouldReturnOrderedRsvps()
    {
        // Arrange
        var rsvps = new List<Rsvp>
        {
            new Rsvp { Id = 1, EventId = TestEventId, Name = "John", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Rsvp { Id = 2, EventId = TestEventId, Name = "Jane", CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new Rsvp { Id = 3, EventId = TestEventId, Name = "Bob", CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvps);

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Bob"); // Newest first
        result[1].Name.Should().Be("Jane");
        result[2].Name.Should().Be("John");
        _repositoryMock.Verify(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenNoRsvps_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rsvp>());

        // Act
        var result = await _handler.HandleAsync(TestEventId);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task HandleAsync_ShouldOnlyReturnRsvpsForSpecifiedEvent()
    {
        // Arrange
        var event1Rsvps = new List<Rsvp>
        {
            new Rsvp { Id = 1, EventId = TestEventId, Name = "John", CreatedAt = DateTime.UtcNow }
        };

        var event2Rsvps = new List<Rsvp>
        {
            new Rsvp { Id = 2, EventId = 2, Name = "Jane", CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(event1Rsvps);

        _repositoryMock
            .Setup(r => r.GetByEventIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(event2Rsvps);

        // Act
        var result1 = await _handler.HandleAsync(TestEventId);
        var result2 = await _handler.HandleAsync(2);

        // Assert
        result1.Should().HaveCount(1);
        result1.First().Name.Should().Be("John");
        result2.Should().HaveCount(1);
        result2.First().Name.Should().Be("Jane");
    }

    [Test]
    public async Task HandleAsync_ShouldMapToRsvpResponse()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Id = 1,
            EventId = TestEventId,
            Name = "John Doe",
            Status = RsvpStatus.Yes,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByEventIdAsync(TestEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rsvp> { rsvp });

        // Act
        var result = (await _handler.HandleAsync(TestEventId)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeOfType<RsvpResponse>();
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("John Doe");
        result[0].Status.Should().Be("Yes");
    }
}
