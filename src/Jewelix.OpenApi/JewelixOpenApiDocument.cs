namespace Jewelix.OpenApi;

/// <summary>
/// Configuration for a single OpenAPI document exposed by <c>Jewelix.OpenApi</c>.
/// Each document gets its own JSON endpoint (<c>/openapi/{Name}.json</c>) and
/// a Scalar UI page (<c>/{ScalarRoutePrefix}/{Name}</c>).
/// </summary>
public sealed class JewelixOpenApiDocument
{
    /// <summary>
    /// Document identifier used in the JSON URL: <c>/openapi/{Name}.json</c>.
    /// Must be unique within <see cref="JewelixOpenApiOptions.Documents"/>.
    /// Default: <c>"v1"</c>.
    /// </summary>
    public string Name { get; set; } = "v1";

    /// <summary>
    /// API title displayed in the Scalar UI heading and the OpenAPI <c>info.title</c> field.
    /// Default: <c>"API"</c>.
    /// </summary>
    public string Title { get; set; } = "API";

    /// <summary>
    /// API version displayed in the Scalar UI and the OpenAPI <c>info.version</c> field.
    /// Default: <c>"1.0"</c>.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Optional markdown description written into the OpenAPI <c>info.description</c> field.
    /// When <see langword="null"/> the field is omitted from the generated document.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When <see langword="true"/>, injects a Bearer/JWT HTTP security scheme into the
    /// OpenAPI document and enables the token input field in the Scalar UI.
    /// Must be set in code at <c>AddJewelixOpenApi</c> time —
    /// config-section overrides do not affect transformer registration.
    /// Default: <see langword="false"/>.
    /// </summary>
    public bool EnableBearerAuth { get; set; }

    /// <summary>
    /// Route prefix for the Scalar UI page. The UI is served at
    /// <c>/{ScalarRoutePrefix}/{Name}</c>.
    /// Default: <c>"scalar"</c> → <c>/scalar/v1</c>.
    /// </summary>
    public string ScalarRoutePrefix { get; set; } = "scalar";
}
