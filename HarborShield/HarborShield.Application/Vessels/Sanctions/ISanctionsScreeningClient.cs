namespace HarborShield.Application.Vessels.Sanctions;

public interface ISanctionsScreeningClient
{
    Task<SanctionsScreeningResult> ScreenAsync(SanctionsScreeningRequest request, CancellationToken cancellationToken);
}
