using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class DeleteEventHandlerTests
{
    private Mock<IEventRepository> _eventRepositoryMock = null!;
    private Mock<IRsvpRepository> _rsvpRepositoryMock = null!;
    private Mock<IPollRepository> _pollRepositoryMock = null!;
    private DeleteEventHandler _handler = null!;
    private const int ValidEventId = 1;
    private const int NonExistentEventId = 999;

    [SetUp]
    public void SetUp()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _rsvpRepositoryMock = new Mock<IRsvpRepository>();
        _pollRepositoryMock = new Mock<IPollRepository>();
        
        // Setup default: event exists, no RSVPs
        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Event { Id = ValidEventId, Title = "Test Event" });
        
        _rsvpRepositoryMock
            .Setup(r => r.GetByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IEnumerable<Rsvp>>(Enumerable.Empty<Rsvp>()));
        
        _pollRepositoryMock
            .Setup(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _eventRepositoryMock
            .Setup(r => r.DeleteAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _handler = new DeleteEventHandler(_eventRepositoryMock.Object, _rsvpRepositoryMock.Object, _pollRepositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenEventExistsAndNoRsvps_ShouldReturnTrue()
    {
        // Arrange
        _rsvpRepositoryMock
            .Setup(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(ValidEventId);

        // Assert
        result.Should().BeTrue();
        _eventRepositoryMock.Verify(r => r.GetByIdAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
        _rsvpRepositoryMock.Verify(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
        _pollRepositoryMock.Verify(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
        _eventRepositoryMock.Verify(r => r.DeleteAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenEventDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.HandleAsync(NonExistentEventId); 

        // Assert
        result.Should().BeFalse();
        _eventRepositoryMock.Verify(r => r.GetByIdAsync(NonExistentEventId, It.IsAny<CancellationToken>()), Times.Once);
        _rsvpRepositoryMock.Verify(r => r.GetByEventIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenCalled_ShouldPassCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(ValidEventId, cancellationToken))
            .ReturnsAsync(new Event { Id = ValidEventId, Title = "Test Event" });
        
        _rsvpRepositoryMock
            .Setup(r => r.DeleteByEventIdAsync(ValidEventId, cancellationToken))
            .Returns(Task.CompletedTask);
        
        _pollRepositoryMock
            .Setup(r => r.DeleteByEventIdAsync(ValidEventId, cancellationToken))
            .ReturnsAsync(true);
        
        _eventRepositoryMock
            .Setup(r => r.DeleteAsync(ValidEventId, cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(ValidEventId, cancellationToken);

        // Assert
        result.Should().BeTrue();
        _rsvpRepositoryMock.Verify(r => r.DeleteByEventIdAsync(ValidEventId, cancellationToken), Times.Once);
        _pollRepositoryMock.Verify(r => r.DeleteByEventIdAsync(ValidEventId, cancellationToken), Times.Once);
        _eventRepositoryMock.Verify(r => r.DeleteAsync(ValidEventId, cancellationToken), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenEventHasRsvps_ShouldDeleteRsvpsAndEvent()
    {
        // Arrange
        var eventWithRsvps = new Event { Id = ValidEventId, Title = "Test Event" };
        var rsvps = new List<Rsvp>
        {
            new Rsvp { Id = 1, EventId = ValidEventId, Name = "John Doe", WillAttend = true }
        };
        
        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventWithRsvps);
        
        _rsvpRepositoryMock
            .Setup(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _pollRepositoryMock
            .Setup(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _eventRepositoryMock
            .Setup(r => r.DeleteAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(ValidEventId);

        // Assert
        result.Should().BeTrue();
        _rsvpRepositoryMock.Verify(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
        _pollRepositoryMock.Verify(r => r.DeleteByEventIdAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
        _eventRepositoryMock.Verify(r => r.DeleteAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleAsync_WhenIdIsZero_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await _handler.HandleAsync(0);
        act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("id");
    }

    [Test]
    public void HandleAsync_WhenIdIsNegative_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await _handler.HandleAsync(-1);
        act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("id");
    }
}
