namespace Jewelix.HealthChecks;

/// <summary>
/// Root configuration options for <c>Jewelix.HealthChecks</c>.
/// Pass an optional <c>IConfiguration</c> and/or configure delegate to
/// <c>AddJewelixHealthChecks</c>.
/// <see cref="ConnectionString"/> and <see cref="Services"/> are bindable from the
/// root of <c>appsettings.json</c>. All endpoint paths and the authorization policy
/// name are fixed constants.
/// </summary>
public sealed class JewelixHealthCheckOptions
{
    // ── Constants ─────────────────────────────────────────────────────────

    /// <summary>Path for the liveness endpoint. Value: <c>/healthz/live</c>.</summary>
    public const string LivenessPath = "/healthz/live";

    /// <summary>Path for the readiness endpoint. Value: <c>/healthz/ready</c>.</summary>
    public const string ReadinessPath = "/healthz/ready";

    /// <summary>
    /// Path of the internal health data feed consumed by the HealthCheckUI dashboard.
    /// Intentionally unprotected — restrict at the network/host level if required.
    /// Value: <c>/healthz/ui-feed</c>.
    /// </summary>
    public const string UiFeedPath = "/healthz/ui-feed";

    /// <summary>Path for the HealthCheckUI dashboard page. Value: <c>/healthchecks-ui</c>.</summary>
    public const string UiPath = "/healthchecks-ui";

    /// <summary>
    /// Authorization policy name applied to <see cref="LivenessPath"/>,
    /// <see cref="ReadinessPath"/>, and <see cref="UiPath"/>.
    /// The calling application must register a matching policy
    /// (e.g. <c>services.AddAuthorization(o => o.AddPolicy("Admin", p => p.RequireRole("Admin")))</c>).
    /// Value: <c>"Admin"</c>.
    /// </summary>
    public const string AdminPolicyName = "Admin";

    /// <summary>
    /// Display name for the SQL Server health check shown in HealthCheckUI.
    /// Value: <c>"sql-server"</c>.
    /// </summary>
    public const string ConnectionCheckName = "sql-server";

    // ── Config-bindable ───────────────────────────────────────────────────

    /// <summary>
    /// SQL Server connection string. When non-null a SQL Server health check
    /// tagged <c>"ready"</c> is registered automatically.
    /// Bound from the root <c>ConnectionString</c> key of <c>appsettings.json</c>.
    /// Default: <see langword="null"/> (SQL check disabled).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// External service endpoints to probe. Each entry is registered as a URL-group
    /// health check tagged <c>"ready"</c>. The library appends <c>/health</c> to each
    /// <see cref="JewelixServiceCheck.Uri"/> to form the probe URL.
    /// Bound from the root <c>Services</c> array of <c>appsettings.json</c>.
    /// </summary>
    public List<JewelixServiceCheck> Services { get; set; } = [];
}
