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
    private const int ValidEventId = 1;
    private const int NonExistentEventId = 999;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IEventRepository>();
        _handler = new DeleteEventHandler(_repositoryMock.Object);
    }

    [Test]
    public void Constructor_WhenRepositoryIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new DeleteEventHandler(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Test]
    public async Task HandleAsync_WhenEventExists_ShouldReturnTrue()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteAsync(ValidEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(ValidEventId);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(ValidEventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenEventDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteAsync(NonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.HandleAsync(NonExistentEventId);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(NonExistentEventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenCalled_ShouldPassCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _repositoryMock
            .Setup(r => r.DeleteAsync(ValidEventId, cancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(ValidEventId, cancellationToken);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(ValidEventId, cancellationToken), Times.Once);
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
