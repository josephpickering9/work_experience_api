using FluentValidation;
using Work_Experience_Search.Requests;

namespace Work_Experience_Search.Validators;

public class CreateTagValidator : AbstractValidator<CreateTag>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Icon).MaximumLength(200).When(x => x.Icon != null);
        RuleFor(x => x.CustomColour).Matches(@"^#[0-9A-Fa-f]{6}$").When(x => x.CustomColour != null)
            .WithMessage("CustomColour must be a valid hex colour (e.g. #FF5733).");
    }
}
