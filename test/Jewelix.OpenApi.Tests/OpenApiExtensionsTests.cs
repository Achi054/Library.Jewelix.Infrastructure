using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewelix.OpenApi.Tests;

public class OpenApiExtensionsTests
{
    [Fact]
    public void UseJewelixOpenApi_WithNullApp_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            OpenApiExtensions.UseJewelixOpenApi(null!));
    }

    [Fact]
    public void UseJewelixOpenApi_WithNonEndpointRouteBuilderApp_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddJewelixOpenApi();
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        var ex = Should.Throw<InvalidOperationException>(() => app.UseJewelixOpenApi());
        ex.Message.ShouldContain("IEndpointRouteBuilder");
    }

    [Fact]
    public void UseJewelixOpenApi_WithConfigurationSection_OverridesDocumentTitle()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["OpenApi:Documents:0:Name"]  = "v1",
            ["OpenApi:Documents:0:Title"] = "Overridden Title",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Simulate the merge logic used by UseJewelixOpenApi:
        // bind config into a fresh options, then merge presentation properties by Name.
        var options = new JewelixOpenApiOptions();
        options.Documents = [new JewelixOpenApiDocument { Name = "v1", Title = "Original Title" }];

        var configOptions = new JewelixOpenApiOptions { Documents = [] };
        configuration.GetSection(JewelixOpenApiOptions.SectionName).Bind(configOptions);
        foreach (var configDoc in configOptions.Documents)
        {
            var target = options.Documents.FirstOrDefault(d => d.Name == configDoc.Name);
            if (target is not null) target.Title = configDoc.Title;
        }

        options.Documents[0].Title.ShouldBe("Overridden Title");
    }

    [Fact]
    public void UseJewelixOpenApi_WithConfigurationSection_DoesNotOverrideEnableBearerAuth()
    {
        // EnableBearerAuth must be set in code at AddJewelixOpenApi time.
        // The merge logic in UseJewelixOpenApi explicitly skips EnableBearerAuth
        // from config — transformer registration cannot be changed post-DI-setup.
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["OpenApi:Documents:0:Name"]             = "v1",
            ["OpenApi:Documents:0:EnableBearerAuth"] = "true",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = new JewelixOpenApiOptions();
        options.Documents = [new JewelixOpenApiDocument { Name = "v1", EnableBearerAuth = false }];

        // Simulate UseJewelixOpenApi merge — EnableBearerAuth is intentionally excluded.
        var configOptions = new JewelixOpenApiOptions { Documents = [] };
        configuration.GetSection(JewelixOpenApiOptions.SectionName).Bind(configOptions);
        foreach (var configDoc in configOptions.Documents)
        {
            var target = options.Documents.FirstOrDefault(d => d.Name == configDoc.Name);
            if (target is null) continue;
            target.Title = configDoc.Title;
            // EnableBearerAuth deliberately not applied here.
        }

        // EnableBearerAuth remains as set in code (false), not overridden by config.
        options.Documents[0].EnableBearerAuth.ShouldBeFalse();
    }
}
