using Jewelix.Logging.Tests.Helper;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace Jewelix.Logging.Tests;

/// <summary>
/// Tests for <see cref="LoggerExtensions.UseJewelixLogger"/>: Serilog logger
/// configuration from an <c>appsettings.json</c>-style in-memory configuration,
/// middleware pipeline registration, and null-argument guard behaviour.
/// </summary>
[Collection(SerilogTestCollection.Name)]
public class UseJewelixLoggerTests : IDisposable
{
    private readonly Serilog.ILogger _previous = Log.Logger;

    public void Dispose()
    {
        (Log.Logger as IDisposable)?.Dispose();
        Log.Logger = _previous;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UseJewelixLogger_WhenSerilogSectionPresent_ReadsSerilogConfiguration()
    {
        // Provide only the minimum-level section. UseJewelixLogger always wires a
        // Console sink in code; adding WriteTo:Console here too would produce a
        // second Console sink during tests.
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information",
        };

        using var host = new HostBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(inMemorySettings))
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s => s.AddJewelixLogger());
                web.Configure((ctx, app) =>
                {
                    app.UseJewelixLogger(ctx.Configuration);
                    app.Run(http => http.Response.WriteAsync("ok"));
                });
            })
            .Build();

        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.ShouldBeTrue();
        body.ShouldBe("ok");
        Log.Logger.ShouldNotBeNull();
    }

    [Fact]
    public void UseJewelixLogger_WithNullApp_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            LoggerExtensions.UseJewelixLogger(null!, new ConfigurationBuilder().Build()));
    }

    [Fact]
    public void UseJewelixLogger_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        Should.Throw<ArgumentNullException>(() => app.UseJewelixLogger(null!));
    }
}
