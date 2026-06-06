using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Enums;
using EventRsvp.Domain.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace EventRsvp.Domain.Tests.Entities;

[TestFixture]
public class RsvpTests
{
    [Test]
    public void Validate_WhenEventIdIsZero_ShouldThrowInvalidRsvpException()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            EventId = 0,
            Name = "John Doe",
            Status = RsvpStatus.Yes
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().Throw<InvalidRsvpException>()
            .WithMessage("*Event ID is required and must be greater than zero*");
    }

    [Test]
    public void Validate_WhenEventIdIsNegative_ShouldThrowInvalidRsvpException()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            EventId = -1,
            Name = "John Doe",
            Status = RsvpStatus.Yes
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().Throw<InvalidRsvpException>()
            .WithMessage("*Event ID is required and must be greater than zero*");
    }

    [Test]
    public void Validate_WhenNameIsEmpty_ShouldThrowInvalidRsvpException()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            EventId = 1,
            Name = string.Empty,
            Status = RsvpStatus.Yes
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().Throw<InvalidRsvpException>()
            .WithMessage("Name is required and cannot be empty.");
    }

    [Test]
    public void Validate_WhenNameIsWhitespace_ShouldThrowInvalidRsvpException()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            EventId = 1,
            Name = "   ",
            Status = RsvpStatus.Yes
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().Throw<InvalidRsvpException>()
            .WithMessage("Name is required and cannot be empty.");
    }

    [Test]
    public void Validate_WhenValidRsvp_ShouldNotThrow()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            EventId = 1,
            Name = "John Doe",
            Status = RsvpStatus.Yes
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().NotThrow();
    }

    [Test]
    public void Validate_WhenValidRsvpNotAttending_ShouldNotThrow()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            EventId = 1,
            Name = "John Doe",
            Status = RsvpStatus.No
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().NotThrow();
    }
}

