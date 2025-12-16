using EventRsvp.Application.DTOs;
using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class CreateRsvpHandlerTests
{
    private Mock<IRsvpRepository> _repositoryMock = null!;
    private CreateRsvpHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IRsvpRepository>();
        _handler = new CreateRsvpHandler(_repositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenValidRequest_ShouldReturnRsvpResponse()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string> { "Pasta Salad" },
            WhiteElephant = true
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string> { "Pasta Salad" },
            WhiteElephant = true,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("John Doe");
        result.BringingDish.Should().BeTrue();
        result.Dishes.Should().Contain("Pasta Salad");
        result.WhiteElephant.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenNameHasWhitespace_ShouldTrimName()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "  John Doe  ",
            BringingDish = false
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            Name = "John Doe",
            BringingDish = false,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.Name == "John Doe"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenNotBringingDish_ShouldFilterOutEmptyDishes()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = "John Doe",
            BringingDish = false,
            Dishes = new List<string> { "Some Dish" }
        };

        var expectedRsvp = new Rsvp
        {
            Id = 1,
            Name = "John Doe",
            BringingDish = false,
            Dishes = new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Rsvp>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRsvp);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<Rsvp>(rsvp => rsvp.Dishes.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleAsync_WhenInvalidRsvp_ShouldThrowException()
    {
        // Arrange
        var request = new CreateRsvpRequest
        {
            Name = string.Empty,
            BringingDish = false
        };

        // Act & Assert
        var act = async () => await _handler.HandleAsync(request);
        act.Should().ThrowAsync<Exception>();
    }
}

