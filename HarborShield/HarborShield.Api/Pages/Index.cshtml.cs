using HarborShield.Application.RiskCases.Queries.ListRiskCases;
using HarborShield.Application.Vessels.Queries.ListVessels;
using HarborShield.Contracts.RiskCases;
using HarborShield.Contracts.Vessels;
using Mediator;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HarborShield.Api.Pages;

public class IndexModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<VesselResponse> Vessels { get; private set; } = [];
    public IReadOnlyList<RiskCaseResponse> RiskCases { get; private set; } = [];
    public IReadOnlyDictionary<Guid, string> VesselNames { get; private set; } = new Dictionary<Guid, string>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Vessels = await mediator.Send(new ListVesselsQuery(), cancellationToken);
        RiskCases = await mediator.Send(new ListRiskCasesQuery(null), cancellationToken);
        VesselNames = Vessels.ToDictionary(v => v.Id, v => v.Name);
    }
}
