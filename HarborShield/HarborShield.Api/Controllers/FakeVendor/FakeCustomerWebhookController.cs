using HarborShield.Application.Common.Interfaces;
using HarborShield.Application.Common.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HarborShield.Api.Controllers.FakeVendor;

/// <summary>
/// Stands in for a real customer's webhook receiver so WebhookDeliveryWorker's HMAC signing
/// and retry logic has something realistic to talk to locally (including signature
/// verification and simulated outages). A real customer endpoint is external code we don't
/// control, so unlike the rest of the Api this reads IApplicationDbContext directly instead
/// of going through Application/MediatR - it's a test double, not a business endpoint.
/// </summary>
[ApiController]
[Route("api/fake-customer/webhook-receiver")]
public class FakeCustomerWebhookController(IApplicationDbContext db) : ControllerBase
{
    [HttpPost("{endpointId:guid}")]
    public async Task<IActionResult> Receive(Guid endpointId, CancellationToken cancellationToken)
    {
        var endpoint = await db.WebhookEndpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == endpointId, cancellationToken);

        if (endpoint is null)
            return NotFound();

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        if (!Request.Headers.TryGetValue("X-HarborShield-Signature", out var signatureHeader))
            return Unauthorized("Missing signature header.");

        if (!HmacSigner.Verify(endpoint.Secret, rawBody, signatureHeader.ToString()))
            return Unauthorized("Signature verification failed.");

        // Simulate an unreliable customer endpoint so the delivery worker's retry logic
        // actually gets exercised, same as the fake sanctions vendor.
        if (Random.Shared.NextDouble() < 0.15)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Ok();
    }
}
