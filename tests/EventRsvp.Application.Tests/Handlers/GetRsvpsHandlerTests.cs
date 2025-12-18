using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class GetRsvpsHandlerTests
{
    private Mock<IRsvpRepository> _repositoryMock = null!;
    private GetRsvpsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IRsvpRepository>();
        _handler = new GetRsvpsHandler(_repositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenRsvpsExist_ShouldReturnOrderedRsvps()
    {
        // Arrange
        var rsvps = new List<Rsvp>
        {
            new Rsvp { Id = 1, Name = "John", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Rsvp { Id = 2, Name = "Jane", CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new Rsvp { Id = 3, Name = "Bob", CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvps);

        // Act
        var result = (await _handler.HandleAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Bob"); // Newest first
        result[1].Name.Should().Be("Jane");
        result[2].Name.Should().Be("John");
    }

    [Test]
    public async Task HandleAsync_WhenNoRsvps_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rsvp>());

        // Act
        var result = await _handler.HandleAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task HandleAsync_ShouldMapToRsvpResponse()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Id = 1,
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string> { "Pasta" },
            WhiteElephant = true,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rsvp> { rsvp });

        // Act
        var result = (await _handler.HandleAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeOfType<RsvpResponse>();
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("John Doe");
    }
}

