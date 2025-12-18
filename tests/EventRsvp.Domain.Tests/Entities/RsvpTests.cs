using EventRsvp.Domain.Entities;
using EventRsvp.Domain.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace EventRsvp.Domain.Tests.Entities;

[TestFixture]
public class RsvpTests
{
    [Test]
    public void Validate_WhenNameIsEmpty_ShouldThrowInvalidRsvpException()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Name = string.Empty,
            BringingDish = false
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
            Name = "   ",
            BringingDish = false
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().Throw<InvalidRsvpException>()
            .WithMessage("Name is required and cannot be empty.");
    }

    [Test]
    public void Validate_WhenBringingDishButNoDishes_ShouldThrowInvalidRsvpException()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string>()
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().Throw<InvalidRsvpException>()
            .WithMessage("If bringing a dish, at least one dish name is required.");
    }

    [Test]
    public void Validate_WhenBringingDishButAllDishesAreEmpty_ShouldThrowInvalidRsvpException()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string> { "", "   ", null! }
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().Throw<InvalidRsvpException>()
            .WithMessage("If bringing a dish, at least one dish name is required.");
    }

    [Test]
    public void Validate_WhenValidRsvp_ShouldNotThrow()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Name = "John Doe",
            BringingDish = false,
            WhiteElephant = true
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().NotThrow();
    }

    [Test]
    public void Validate_WhenBringingDishWithValidDishes_ShouldNotThrow()
    {
        // Arrange
        var rsvp = new Rsvp
        {
            Name = "John Doe",
            BringingDish = true,
            Dishes = new List<string> { "Pasta Salad", "Brownies" }
        };

        // Act & Assert
        var act = () => rsvp.Validate();
        act.Should().NotThrow();
    }
}

