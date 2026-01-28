using EventRsvp.Application.DTOs;
using FluentValidation;

namespace EventRsvp.Application.Validators;

public class CreatePollRequestValidator : AbstractValidator<CreatePollRequest>
{
    public CreatePollRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required.")
            .Must(question => !string.IsNullOrWhiteSpace(question)).WithMessage("Question cannot be empty or whitespace.");

        RuleFor(x => x.Options)
            .NotEmpty().WithMessage("Options are required.")
            .Must(options => options != null && options.Count >= 2).WithMessage("At least 2 options are required.")
            .ForEach(option => option
                .NotEmpty().WithMessage("Option cannot be empty.")
                .Must(opt => !string.IsNullOrWhiteSpace(opt)).WithMessage("Option cannot be empty or whitespace."));
    }
}
