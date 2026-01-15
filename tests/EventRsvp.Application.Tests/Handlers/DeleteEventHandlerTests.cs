using EventRsvp.Application.Handlers;
using EventRsvp.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Handlers;

[TestFixture]
public class DeleteEventHandlerTests
{
    private Mock<IEventRepository> _repositoryMock = null!;
    private DeleteEventHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IEventRepository>();
        _handler = new DeleteEventHandler(_repositoryMock.Object);
    }

    [Test]
    public async Task HandleAsync_WhenEventExists_ShouldReturnTrue()
    {
        // Arrange
        var eventId = 1;
        _repositoryMock
            .Setup(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(eventId);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenEventDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var eventId = 999;
        _repositoryMock
            .Setup(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.HandleAsync(eventId);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenCalled_ShouldPassCancellationToken()
    {
        // Arrange
        var eventId = 1;
        var cancellationToken = new CancellationToken();
        _repositoryMock
            .Setup(r => r.DeleteAsync(eventId, cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(eventId, cancellationToken);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(eventId, cancellationToken), Times.Once);
    }
}
