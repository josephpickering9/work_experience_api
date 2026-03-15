using FluentValidation;
using Work_Experience_Search.Requests;

namespace Work_Experience_Search.Validators;

public class CreateProjectValidator : AbstractValidator<CreateProject>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(50000);
        RuleFor(x => x.Website).MaximumLength(500).When(x => x.Website != null);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleForEach(x => x.Repositories).ChildRules(repo =>
        {
            repo.RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
            repo.RuleFor(r => r.Url).NotEmpty().MaximumLength(500);
        });
    }
}
