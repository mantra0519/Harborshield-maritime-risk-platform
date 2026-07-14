using FluentValidation;

namespace HarborShield.Application.RiskCases.Commands.CreateRiskCase;

public class CreateRiskCaseCommandValidator : AbstractValidator<CreateRiskCaseCommand>
{
    public CreateRiskCaseCommandValidator()
    {
        RuleFor(c => c.VesselId).NotEmpty();
        RuleFor(c => c.CaseType).IsInEnum();
        RuleFor(c => c.Severity).IsInEnum();
        RuleFor(c => c.RiskScore).InclusiveBetween(0, 100);
        RuleFor(c => c.Reasons).NotEmpty();
    }
}
