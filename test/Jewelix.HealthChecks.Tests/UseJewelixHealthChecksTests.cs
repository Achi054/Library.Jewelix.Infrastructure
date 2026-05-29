using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewelix.HealthChecks.Tests;

public class UseJewelixHealthChecksTests
{
    [Fact]
    public void UseJewelixHealthChecks_WithNullApp_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckExtensions.UseJewelixHealthChecks(null!));
    }

    [Fact]
    public void UseJewelixHealthChecks_WithNonRouteBuilder_ThrowsInvalidOperationException()
    {
        // ApplicationBuilder implements IApplicationBuilder but NOT IEndpointRouteBuilder,
        // exercising the guard that requires a WebApplication instance.
        var appBuilder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        Should.Throw<InvalidOperationException>(() =>
            HealthCheckExtensions.UseJewelixHealthChecks(appBuilder));
    }

    [Fact]
    public async Task UseJewelixHealthChecks_WithDefaultOptions_LivenessEndpointReturns200()
    {
        await using var app = BuildTestApp();
        await app.StartAsync();

        var response = await app.GetTestClient()
            .GetAsync(new Uri(JewelixHealthCheckOptions.LivenessPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UseJewelixHealthChecks_WithDefaultOptions_ReadinessEndpointReturns200()
    {
        await using var app = BuildTestApp();
        await app.StartAsync();

        var response = await app.GetTestClient()
            .GetAsync(new Uri(JewelixHealthCheckOptions.ReadinessPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UseJewelixHealthChecks_WithDefaultOptions_UiFeedEndpointReturns200()
    {
        await using var app = BuildTestApp();
        await app.StartAsync();

        var response = await app.GetTestClient()
            .GetAsync(new Uri(JewelixHealthCheckOptions.UiFeedPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task UseJewelixHealthChecks_UiFeedResponse_ContainsHealthCheckJson()
    {
        await using var app = BuildTestApp();
        await app.StartAsync();

        var content = await app.GetTestClient()
            .GetStringAsync(new Uri(JewelixHealthCheckOptions.UiFeedPath, UriKind.Relative));

        // UIResponseWriter produces a JSON object with "status" at the root.
        content.ShouldContain("status");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static WebApplication BuildTestApp()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddJewelixHealthChecks();
        builder.Services.AddAuthorization(opts =>
            opts.AddPolicy("Admin", p => p.RequireAssertion(_ => true)));
        var app = builder.Build();
        app.UseAuthorization();
        app.UseJewelixHealthChecks();
        return app;
    }
}
