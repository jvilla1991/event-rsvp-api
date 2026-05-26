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
    }
}
