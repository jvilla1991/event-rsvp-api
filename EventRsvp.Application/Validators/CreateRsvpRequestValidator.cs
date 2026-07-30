using EventRsvp.Application.DTOs;
using FluentValidation;

namespace EventRsvp.Application.Validators;

public class CreateRsvpRequestValidator : AbstractValidator<CreateRsvpRequest>
{
    public CreateRsvpRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name cannot be empty or whitespace.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        // All wedding fields are optional — rules only apply when a value is supplied,
        // so requests from events that don't use them remain valid.
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(320).WithMessage("Email cannot exceed 320 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.GuestCount)
            .InclusiveBetween(1, 4).WithMessage("Guest count must be between 1 and 4.")
            .When(x => x.GuestCount.HasValue);

        RuleFor(x => x.MealChoice)
            .MaximumLength(100).WithMessage("Meal choice cannot exceed 100 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Note cannot exceed 1000 characters.");
    }
}
