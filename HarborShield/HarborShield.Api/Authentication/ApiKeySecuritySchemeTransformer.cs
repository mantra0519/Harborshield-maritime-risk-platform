using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace HarborShield.Api.Authentication;

/// <summary>
/// Documents the API key requirement in the OpenAPI spec so Scalar shows there's an "ApiKey"
/// scheme available to authorize with, instead of the caller having to guess the header name.
/// </summary>
public class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationOptions.HeaderName,
            Description = "API key required for all endpoints except the fake-vendor/fake-customer test doubles."
        };

        return Task.CompletedTask;
    }
}
