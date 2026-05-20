using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Jewelix.OpenApi;

/// <summary>
/// Injects a Bearer/JWT HTTP security scheme into the OpenAPI document components.
/// Registered automatically by <c>AddJewelixOpenApi</c> for every document with
/// <see cref="JewelixOpenApiDocument.EnableBearerAuth"/> set to <see langword="true"/>.
/// Idempotent — safe to call multiple times.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter your JWT bearer token."
        };

        return Task.CompletedTask;
    }
}
