using FluentValidation;

namespace HarborShield.Application.Vessels.Commands.RegisterVessel;

public class RegisterVesselCommandValidator : AbstractValidator<RegisterVesselCommand>
{
    public RegisterVesselCommandValidator()
    {
        RuleFor(c => c.ImoNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.FlagCountry).NotEmpty().MaximumLength(100);
    }
}
