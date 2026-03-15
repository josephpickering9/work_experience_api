using FluentValidation;
using Work_Experience_Search.Requests;

namespace Work_Experience_Search.Validators;

public class CreateCompanyValidator : AbstractValidator<CreateCompany>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Website).MaximumLength(500).When(x => x.Website != null);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
