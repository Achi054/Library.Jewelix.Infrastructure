using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Jewelix.OpenApi;

/// <summary>
/// DI and pipeline registration for the Jewelix OpenAPI library.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Registers OpenAPI document generation services for all configured documents
    /// and adds the <see cref="JewelixOpenApiOptions"/> singleton to the DI container.
    /// Call <see cref="UseJewelixOpenApi"/> in the middleware pipeline to expose the
    /// generated JSON endpoints and Scalar UI routes.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">
    /// Optional delegate to configure <see cref="JewelixOpenApiOptions"/>. When omitted
    /// a single document named <c>"v1"</c> is registered with default settings.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddJewelixOpenApi(
        this IServiceCollection services,
        Action<JewelixOpenApiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new JewelixOpenApiOptions();
        configure?.Invoke(options);

        foreach (var document in options.Documents)
        {
            var doc = document; // capture for lambda
            services.AddOpenApi(doc.Name, openApiOptions =>
            {
                openApiOptions.AddDocumentTransformer((openApiDoc, _, _) =>
                {
                    openApiDoc.Info.Title   = doc.Title;
                    openApiDoc.Info.Version = doc.Version;
                    if (doc.Description is not null)
                        openApiDoc.Info.Description = doc.Description;
                    return Task.CompletedTask;
                });

                if (doc.EnableBearerAuth)
                    openApiOptions.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });
        }

        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Maps the OpenAPI JSON endpoints (<c>/openapi/{name}.json</c>) and Scalar UI
    /// routes (<c>/{prefix}/{name}</c>) for every document registered via
    /// <see cref="AddJewelixOpenApi"/>. Optionally re-binds the <c>OpenApi</c>
    /// configuration section on top of the code-level options, enabling per-environment
    /// overrides of <c>Title</c>, <c>Version</c>, <c>Description</c>, and
    /// <c>ScalarRoutePrefix</c> without code changes.
    /// </summary>
    /// <param name="app">
    /// The application builder. Must implement <see cref="IEndpointRouteBuilder"/>
    /// (i.e. be a <c>WebApplication</c> instance).
    /// </param>
    /// <param name="configuration">
    /// Optional configuration root. When provided the <c>OpenApi</c> section is bound
    /// over the existing options. Only presentation properties are overridden —
    /// <see cref="JewelixOpenApiDocument.EnableBearerAuth"/> is ignored here because
    /// the corresponding transformer must be registered at <see cref="AddJewelixOpenApi"/> time.
    /// </param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="app"/> does not implement <see cref="IEndpointRouteBuilder"/>.
    /// </exception>
    public static IApplicationBuilder UseJewelixOpenApi(
        this IApplicationBuilder app,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app is not IEndpointRouteBuilder endpointRouteBuilder)
            throw new InvalidOperationException(
                "UseJewelixOpenApi requires an IEndpointRouteBuilder. " +
                "Ensure it is called on a WebApplication instance.");

        var options = app.ApplicationServices.GetRequiredService<JewelixOpenApiOptions>();

        // Optional environment-level override — presentation properties only.
        configuration?.GetSection(JewelixOpenApiOptions.SectionName).Bind(options);

        foreach (var document in options.Documents)
        {
            endpointRouteBuilder.MapOpenApi($"/openapi/{document.Name}.json");

            endpointRouteBuilder.MapScalarApiReference(
                $"/{document.ScalarRoutePrefix}/{document.Name}",
                scalarOptions =>
                {
                    scalarOptions.Title = document.Title;
                    scalarOptions.WithOpenApiRoutePattern(
                        $"/openapi/{document.Name}.json");

                    if (document.EnableBearerAuth)
                        scalarOptions.Authentication = new ScalarAuthenticationOptions
                        {
                            PreferredSecuritySchemes = ["Bearer"]
                        };
                });
        }

        return app;
    }
}
