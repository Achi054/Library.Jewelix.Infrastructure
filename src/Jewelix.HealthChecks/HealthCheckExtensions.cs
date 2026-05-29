using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jewelix.HealthChecks;

/// <summary>
/// DI and pipeline registration for the Jewelix.HealthChecks library.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers health check services: SQL Server check (when <c>ConnectionString</c> is set),
    /// external service checks (from <c>Services</c>), HealthCheckUI with InMemory storage, and
    /// the <see cref="JewelixHealthCheckOptions"/> singleton.
    /// <para>
    /// Merge order: defaults → <paramref name="configure"/> delegate → <paramref name="configuration"/>
    /// root keys (<c>ConnectionString</c> and <c>Services</c>). Config wins over the delegate.
    /// </para>
    /// Call <see cref="UseJewelixHealthChecks"/> in the middleware pipeline to expose endpoints.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">
    /// Optional. Root <c>ConnectionString</c> and <c>Services</c> keys are read and applied on
    /// top of the delegate. A non-empty <c>Services</c> array replaces any delegate-configured list.
    /// </param>
    /// <param name="configure">
    /// Optional delegate to configure <see cref="JewelixHealthCheckOptions"/> in code.
    /// Applied before <paramref name="configuration"/>.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddJewelixHealthChecks(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<JewelixHealthCheckOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new JewelixHealthCheckOptions();
        configure?.Invoke(options);

        // Config wins over delegate for ConnectionString and Services (root keys).
        if (configuration is not null)
        {
            var configConnectionString = configuration["ConnectionString"];
            if (!string.IsNullOrWhiteSpace(configConnectionString))
                options.ConnectionString = configConnectionString;

            var configServices = configuration.GetSection("Services")
                                              .Get<List<JewelixServiceCheck>>();
            if (configServices is { Count: > 0 })
                options.Services = configServices;
        }

        var builder = services.AddHealthChecks();

        // SQL Server check — tagged "ready" so it only runs on the readiness endpoint.
        if (options.ConnectionString is not null)
        {
            builder.AddSqlServer(
                connectionString: options.ConnectionString,
                healthQuery: "SELECT 1;",
                configure: null,
                name: JewelixHealthCheckOptions.ConnectionCheckName,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);
        }

        // External service checks — /health is appended to each base URI.
        // Each is tagged "ready" so it appears on the readiness endpoint.
        foreach (var service in options.Services)
        {
            if (string.IsNullOrWhiteSpace(service.Uri)) continue;
            var probeUri = new Uri(service.Uri.TrimEnd('/') + "/health", UriKind.Absolute);
            builder.AddUrlGroup(
                uri: probeUri,
                name: string.IsNullOrWhiteSpace(service.Name) ? service.Uri : service.Name,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);
        }

        // HealthCheckUI + InMemory storage.
        services
            .AddHealthChecksUI()
            .AddInMemoryStorage();

        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Maps health check endpoints and the HealthCheckUI dashboard using fixed paths:
    /// <list type="bullet">
    ///   <item><c>/healthz/live</c> — liveness probe (Admin-protected).</item>
    ///   <item><c>/healthz/ready</c> — readiness probe (Admin-protected).</item>
    ///   <item><c>/healthz/ui-feed</c> — internal JSON feed for HealthCheckUI polling (unprotected).</item>
    ///   <item><c>/healthchecks-ui</c> — HealthCheckUI dashboard (Admin-protected).</item>
    /// </list>
    /// All paths and the <c>"Admin"</c> policy name are fixed constants on
    /// <see cref="JewelixHealthCheckOptions"/>.
    /// </summary>
    /// <param name="app">
    /// The application builder. Must implement <see cref="IEndpointRouteBuilder"/>.
    /// </param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="app"/> does not implement <see cref="IEndpointRouteBuilder"/>.
    /// </exception>
    public static IApplicationBuilder UseJewelixHealthChecks(
        this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app is not IEndpointRouteBuilder erb)
            throw new InvalidOperationException(
                "UseJewelixHealthChecks requires an IEndpointRouteBuilder. " +
                "Ensure it is called on a WebApplication instance.");

        // Liveness — only checks tagged "live" (process-alive signal; no dependencies).
        erb.MapHealthChecks(JewelixHealthCheckOptions.LivenessPath, new HealthCheckOptions
        {
            Predicate      = r => r.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        }).RequireAuthorization(JewelixHealthCheckOptions.AdminPolicyName);

        // Readiness — checks tagged "ready" (SQL Server, external services).
        erb.MapHealthChecks(JewelixHealthCheckOptions.ReadinessPath, new HealthCheckOptions
        {
            Predicate      = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        }).RequireAuthorization(JewelixHealthCheckOptions.AdminPolicyName);

        // UI data feed — unprotected so the HealthCheckUI dashboard can poll it.
        // Runs ALL registered checks and returns the full JSON report.
        // Restrict access at the network/host level (firewall, RequireHost) if needed.
        erb.MapHealthChecks(JewelixHealthCheckOptions.UiFeedPath, new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        // HealthCheckUI dashboard — Admin-protected.
        erb.MapHealthChecksUI(uiOptions =>
        {
            uiOptions.UIPath = JewelixHealthCheckOptions.UiPath;
        }).RequireAuthorization(JewelixHealthCheckOptions.AdminPolicyName);

        return app;
    }
}
