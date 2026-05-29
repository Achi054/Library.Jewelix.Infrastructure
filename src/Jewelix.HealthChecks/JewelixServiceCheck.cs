namespace Jewelix.HealthChecks;

/// <summary>
/// Configuration for a single external service health check.
/// The library appends <c>/health</c> to <see cref="Uri"/> to form the probe URL.
/// Example: <c>https://payments.example.com</c> → probe: <c>https://payments.example.com/health</c>.
/// Each entry is registered as a URL-group health check tagged <c>"ready"</c>.
/// </summary>
public sealed class JewelixServiceCheck
{
    /// <summary>
    /// Display name for the check shown in HealthCheckUI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the service. The library appends <c>/health</c> to form the probe URI.
    /// Must be an absolute URI (e.g. <c>https://payments.example.com</c>).
    /// Kept as <see langword="string"/> for seamless <c>appsettings.json</c> config binding.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "String type is required for IConfiguration binding from appsettings.json.")]
    public string Uri { get; set; } = string.Empty;
}
