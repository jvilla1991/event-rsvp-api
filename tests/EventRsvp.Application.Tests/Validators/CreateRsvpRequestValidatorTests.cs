using EventRsvp.Application.DTOs;
using EventRsvp.Application.Validators;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace EventRsvp.Application.Tests.Validators;

[TestFixture]
public class CreateRsvpRequestValidatorTests
{
    private CreateRsvpRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateRsvpRequestValidator();
    }

    private static CreateRsvpRequest ValidRequest() => new()
    {
        Name = "John Doe",
        Status = "Yes"
    };

    [Test]
    public void Validate_WhenRequestHasOnlyNameAndStatus_ShouldBeValid()
    {
        // A plain RSVP without any wedding fields must stay valid (backward compatible)
        var request = ValidRequest();

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WhenAllWeddingFieldsNull_ShouldBeValid()
    {
        var request = ValidRequest();
        request.Email = null;
        request.GuestCount = null;
        request.MealChoice = null;
        request.Note = null;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Email = "not-an-email";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Validate_WhenEmailIsValid_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.Email = "john.doe@example.com";

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Validate_WhenGuestCountIsZero_ShouldHaveError()
    {
        var request = ValidRequest();
        request.GuestCount = 0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.GuestCount);
    }

    [Test]
    public void Validate_WhenGuestCountIsFive_ShouldHaveError()
    {
        var request = ValidRequest();
        request.GuestCount = 5;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.GuestCount);
    }

    [Test]
    public void Validate_WhenGuestCountIsOne_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.GuestCount = 1;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.GuestCount);
    }

    [Test]
    public void Validate_WhenGuestCountIsFour_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.GuestCount = 4;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.GuestCount);
    }

    [Test]
    public void Validate_WhenMealChoiceExceedsMaxLength_ShouldHaveError()
    {
        var request = ValidRequest();
        request.MealChoice = new string('x', 101);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MealChoice);
    }

    [Test]
    public void Validate_WhenNoteExceedsMaxLength_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Note = new string('x', 1001);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Test]
    public void Validate_WhenMealChoiceAndNoteWithinLimits_ShouldNotHaveErrors()
    {
        var request = ValidRequest();
        request.MealChoice = "Vegetarian";
        request.Note = "No shellfish, please.";

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
