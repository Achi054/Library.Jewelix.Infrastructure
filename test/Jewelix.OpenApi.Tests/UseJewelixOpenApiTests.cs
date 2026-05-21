using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Jewelix.OpenApi.Tests;

public class UseJewelixOpenApiTests
{
    [Fact]
    public async Task UseJewelixOpenApi_WithDefaultOptions_OpenApiJsonEndpointReturns200()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddJewelixOpenApi();
        await using var app = builder.Build();
        app.UseJewelixOpenApi();
        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task UseJewelixOpenApi_WithDefaultOptions_ScalarUiEndpointReturns200()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddJewelixOpenApi();
        await using var app = builder.Build();
        app.UseJewelixOpenApi();
        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.GetAsync(new Uri("/scalar/v1/", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UseJewelixOpenApi_WithMultipleDocuments_AllJsonEndpointsReturn200()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddJewelixOpenApi(opts =>
        {
            opts.Documents =
            [
                new JewelixOpenApiDocument { Name = "v1", Title = "API v1" },
                new JewelixOpenApiDocument { Name = "v2", Title = "API v2" },
            ];
        });
        await using var app = builder.Build();
        app.UseJewelixOpenApi();
        await app.StartAsync();

        var client = app.GetTestClient();

        (await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync(new Uri("/openapi/v2.json", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UseJewelixOpenApi_WithMultipleDocuments_AllScalarUiEndpointsReturn200()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddJewelixOpenApi(opts =>
        {
            opts.Documents =
            [
                new JewelixOpenApiDocument { Name = "v1", Title = "API v1" },
                new JewelixOpenApiDocument { Name = "v2", Title = "API v2" },
            ];
        });
        await using var app = builder.Build();
        app.UseJewelixOpenApi();
        await app.StartAsync();

        var client = app.GetTestClient();

        (await client.GetAsync(new Uri("/scalar/v1/", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync(new Uri("/scalar/v2/", UriKind.Relative)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UseJewelixOpenApi_WithEnableBearerAuth_OpenApiJsonContainsBearerScheme()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddJewelixOpenApi(opts =>
        {
            opts.Documents = [new JewelixOpenApiDocument { Name = "v1", EnableBearerAuth = true }];
        });
        await using var app = builder.Build();
        app.UseJewelixOpenApi();
        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldContain("bearer");
        content.ShouldContain("JWT");
    }
}
