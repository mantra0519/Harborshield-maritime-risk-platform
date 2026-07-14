using FluentValidation;

namespace HarborShield.Application.RiskCases.Commands.ResolveRiskCase;

public class ResolveRiskCaseCommandValidator : AbstractValidator<ResolveRiskCaseCommand>
{
    public ResolveRiskCaseCommandValidator()
    {
        RuleFor(c => c.RiskCaseId).NotEmpty();
        RuleFor(c => c.Notes).NotEmpty().MaximumLength(2000);
    }
}
