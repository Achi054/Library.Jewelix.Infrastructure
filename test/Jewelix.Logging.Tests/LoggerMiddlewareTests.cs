using Jewelix.Logging.Tests.Helper;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace Jewelix.Logging.Tests;

/// <summary>
/// Tests for <see cref="LoggerMiddleware.Invoke"/>: correlation ID generation and
/// propagation, HTTP summary event emission, request/response body capture at Debug
/// level, and sensitive field masking. Uses an in-process <c>TestServer</c> to drive
/// real HTTP requests through the middleware pipeline.
/// </summary>
[Collection(SerilogTestCollection.Name)]
public class LoggerMiddlewareTests : IDisposable
{
    private readonly InMemorySink _sink = new();
    private readonly Serilog.ILogger _previous;

    public LoggerMiddlewareTests()
    {
        _previous = Log.Logger;
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    public void Dispose()
    {
        (Log.Logger as IDisposable)?.Dispose();
        Log.Logger = _previous;
        GC.SuppressFinalize(this);
    }

    // We register LoggerMiddleware directly rather than calling UseJewelixLogger so the
    // extension doesn't overwrite Log.Logger and clobber our captured sink.
    private static IHost BuildHost(RequestDelegate? handler = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<LoggerMiddleware>();
                    app.Run(handler ?? (async http =>
                    {
                        http.Response.ContentType = "application/json";
                        await http.Response.WriteAsync("""{"hello":"world"}""");
                    }));
                });
            })
            .Build();
    }

    [Fact]
    public async Task Invoke_WhenNoCorrelationIdInRequest_AddsNewCorrelationIdHeader()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync(new Uri("/", UriKind.Relative));

        response.Headers.TryGetValues("X-Correlation-ID", out var values).ShouldBeTrue();
        values!.Single().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Invoke_WhenCorrelationIdProvidedByCaller_ReusesItInResponse()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();

        const string id = "test-correlation-123";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", id);

        var response = await client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-ID").Single().ShouldBe(id);
    }

    [Fact]
    public async Task Invoke_WhenRequestProcessed_LogsSummaryEventWithMethodPathAndStatus()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();

        await client.GetAsync(new Uri("/", UriKind.Relative));

        var summary = _sink.Events.FirstOrDefault(e =>
            e.Properties.ContainsKey("Method") &&
            e.Properties.ContainsKey("StatusCode"));

        summary.ShouldNotBeNull();
        summary!.Properties["Method"].ToString().ShouldContain("GET");
        summary.Properties["StatusCode"].ToString().ShouldBe("200");
    }

    [Fact]
    public async Task Invoke_WhenCorrelationIdProvided_StampsItOnLogEvents()
    {
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();

        const string id = "trace-me-please";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", id);

        await client.SendAsync(request);

        var ev = _sink.Events.FirstOrDefault(e => e.Properties.ContainsKey("CorrelationId"));
        ev.ShouldNotBeNull();
        ev!.Properties["CorrelationId"].ToString().ShouldContain(id);
    }

    [Fact]
    public async Task Invoke_WhenDebugCaptureActive_ReturnsResponseBodyToCaller()
    {
        // Verbose minimum on the sink => Debug is enabled => middleware enters the
        // body-capture branch. The captured stream must still flow back to the client.
        using var host = BuildHost();
        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldBe("""{"hello":"world"}""");
    }

    [Fact]
    public async Task Invoke_WhenDebugCaptureActiveWithSensitiveFields_MasksRequestBodyFields()
    {
        using var host = BuildHost(async http =>
        {
            http.Response.StatusCode = 204;
            await Task.CompletedTask;
        });
        await host.StartAsync();
        var client = host.GetTestClient();

        using var content = new StringContent(
            """{"username":"alice","password":"hunter2"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        await client.PostAsync(new Uri("/", UriKind.Relative), content);

        var requestEvent = _sink.Events.FirstOrDefault(e => e.Properties.ContainsKey("Body"));
        requestEvent.ShouldNotBeNull();
        var body = requestEvent!.Properties["Body"].ToString();
        body.ShouldContain("***MASKED***");
        body.ShouldNotContain("hunter2");
    }
}
