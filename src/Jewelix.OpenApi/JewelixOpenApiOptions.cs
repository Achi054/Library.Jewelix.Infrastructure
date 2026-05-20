namespace Jewelix.OpenApi;

/// <summary>
/// Root configuration options for <c>Jewelix.OpenApi</c>.
/// Pass a configure delegate to <c>AddJewelixOpenApi</c> to
/// set these in code. Override presentation properties at runtime by binding the
/// <c>OpenApi</c> section of <c>appsettings.json</c> in
/// <c>UseJewelixOpenApi</c>.
/// </summary>
public sealed class JewelixOpenApiOptions
{
    /// <summary>
    /// The <c>appsettings.json</c> section name used for config-section override in
    /// <c>UseJewelixOpenApi</c>.
    /// </summary>
    public const string SectionName = "OpenApi";

    /// <summary>
    /// One entry per OpenAPI document to register and expose via the Scalar UI.
    /// Defaults to a single document named <c>"v1"</c> with all default settings.
    /// </summary>
    public List<JewelixOpenApiDocument> Documents { get; set; } =
    [
        new JewelixOpenApiDocument()
    ];
}
