using HarborShield.Application.Common.Exceptions;
using HarborShield.Application.RiskCases.Queries.ExplainRiskCase;
using HarborShield.Application.RiskCases.Queries.GetRiskCaseById;
using HarborShield.Application.Vessels.Queries.GetVesselById;
using HarborShield.Contracts.RiskCases;
using HarborShield.Contracts.Vessels;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HarborShield.Api.Pages.RiskCases;

public class DetailsModel(IMediator mediator) : PageModel
{
    public RiskCaseResponse RiskCase { get; private set; } = default!;
    public VesselResponse Vessel { get; private set; } = default!;
    public bool ExplanationJustGenerated { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            RiskCase = await mediator.Send(new GetRiskCaseByIdQuery(id), cancellationToken);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        Vessel = await mediator.Send(new GetVesselByIdQuery(RiskCase.VesselId), cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostExplainAsync(Guid id, CancellationToken cancellationToken)
    {
        // First call for an uncached case runs live local-LLM inference (can take 10-30s);
        // the result is persisted by the handler, so every view after this one is instant.
        await mediator.Send(new ExplainRiskCaseQuery(id), cancellationToken);
        return RedirectToPage(new { id });
    }
}
