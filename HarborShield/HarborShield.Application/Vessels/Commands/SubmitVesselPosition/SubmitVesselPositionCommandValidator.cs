using FluentValidation;

namespace HarborShield.Application.Vessels.Commands.SubmitVesselPosition;

public class SubmitVesselPositionCommandValidator : AbstractValidator<SubmitVesselPositionCommand>
{
    public SubmitVesselPositionCommandValidator()
    {
        RuleFor(c => c.VesselId).NotEmpty();
        RuleFor(c => c.Latitude).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitude).InclusiveBetween(-180, 180);
        RuleFor(c => c.SpeedKnots).GreaterThanOrEqualTo(0);
        RuleFor(c => c.HeadingDegrees).InclusiveBetween(0, 360);
        RuleFor(c => c.RecordedAt).NotEmpty();
    }
}
