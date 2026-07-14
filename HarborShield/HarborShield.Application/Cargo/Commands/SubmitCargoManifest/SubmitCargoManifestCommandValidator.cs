using FluentValidation;

namespace HarborShield.Application.Cargo.Commands.SubmitCargoManifest;

public class SubmitCargoManifestCommandValidator : AbstractValidator<SubmitCargoManifestCommand>
{
    public SubmitCargoManifestCommandValidator()
    {
        RuleFor(c => c.VesselId).NotEmpty();
        RuleFor(c => c.OriginPort).NotEmpty().MaximumLength(200);
        RuleFor(c => c.DestinationPort).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ShipperName).MaximumLength(200);
        RuleFor(c => c.ReceiverName).MaximumLength(200);
        RuleFor(c => c.DeclaredWeightKg).GreaterThanOrEqualTo(0);
    }
}
