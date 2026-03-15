using FluentValidation;
using Work_Experience_Search.Requests;

namespace Work_Experience_Search.Validators;

public class VertexQueryRequestValidator : AbstractValidator<VertexQueryRequest>
{
    private const int MaxQueryLength = 500;

    public VertexQueryRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(MaxQueryLength)
            .WithMessage($"Query must not exceed {MaxQueryLength} characters.");
    }
}
