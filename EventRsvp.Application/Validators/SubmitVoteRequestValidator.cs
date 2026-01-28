using EventRsvp.Application.DTOs;
using FluentValidation;

namespace EventRsvp.Application.Validators;

public class SubmitVoteRequestValidator : AbstractValidator<SubmitVoteRequest>
{
    public SubmitVoteRequestValidator()
    {
        RuleFor(x => x.SelectedOptions)
            .NotEmpty().WithMessage("SelectedOptions are required.")
            .Must(options => options != null && options.Count > 0).WithMessage("At least one option must be selected.");
    }
}
