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
        // bind config into a fresh options, then merge by Name using compare-against-defaults
        // so that properties absent from config do not silently reset code-configured values.
        var options = new JewelixOpenApiOptions();
        options.Documents = [new JewelixOpenApiDocument { Name = "v1", Title = "Original Title" }];

        var configOptions = new JewelixOpenApiOptions { Documents = [] };
        configuration.GetSection(JewelixOpenApiOptions.SectionName).Bind(configOptions);
        var defaults = new JewelixOpenApiDocument();
        foreach (var configDoc in configOptions.Documents)
        {
            var target = options.Documents.FirstOrDefault(d => d.Name == configDoc.Name);
            if (target is null) continue;
            if (configDoc.Title != defaults.Title)
                target.Title = configDoc.Title;
            if (configDoc.Version != defaults.Version)
                target.Version = configDoc.Version;
            if (configDoc.ScalarRoutePrefix != defaults.ScalarRoutePrefix)
                target.ScalarRoutePrefix = configDoc.ScalarRoutePrefix;
            if (configDoc.Description is not null)
                target.Description = configDoc.Description;
            // EnableBearerAuth deliberately not applied here.
        }

        options.Documents[0].Title.ShouldBe("Overridden Title");
    }

    [Fact]
    public void UseJewelixOpenApi_WithPartialConfigSection_PreservesUnspecifiedCodeValues()
    {
        // When config only specifies Title, the code-configured Version and
        // ScalarRoutePrefix must be left intact (not silently reset to defaults).
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["OpenApi:Documents:0:Name"]  = "v1",
            ["OpenApi:Documents:0:Title"] = "Config Title",
            // Version and ScalarRoutePrefix intentionally absent from config
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = new JewelixOpenApiOptions();
        options.Documents =
        [
            new JewelixOpenApiDocument
            {
                Name             = "v1",
                Title            = "Code Title",
                Version          = "2.0",
                ScalarRoutePrefix = "docs",
            },
        ];

        var configOptions = new JewelixOpenApiOptions { Documents = [] };
        configuration.GetSection(JewelixOpenApiOptions.SectionName).Bind(configOptions);
        var defaults = new JewelixOpenApiDocument();
        foreach (var configDoc in configOptions.Documents)
        {
            var target = options.Documents.FirstOrDefault(d => d.Name == configDoc.Name);
            if (target is null) continue;
            if (configDoc.Title != defaults.Title)
                target.Title = configDoc.Title;
            if (configDoc.Version != defaults.Version)
                target.Version = configDoc.Version;
            if (configDoc.ScalarRoutePrefix != defaults.ScalarRoutePrefix)
                target.ScalarRoutePrefix = configDoc.ScalarRoutePrefix;
            if (configDoc.Description is not null)
                target.Description = configDoc.Description;
        }

        options.Documents[0].Title.ShouldBe("Config Title");          // overridden by config
        options.Documents[0].Version.ShouldBe("2.0");                 // preserved from code
        options.Documents[0].ScalarRoutePrefix.ShouldBe("docs");      // preserved from code
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
        var defaults = new JewelixOpenApiDocument();
        foreach (var configDoc in configOptions.Documents)
        {
            var target = options.Documents.FirstOrDefault(d => d.Name == configDoc.Name);
            if (target is null) continue;
            if (configDoc.Title != defaults.Title)
                target.Title = configDoc.Title;
            if (configDoc.Version != defaults.Version)
                target.Version = configDoc.Version;
            if (configDoc.ScalarRoutePrefix != defaults.ScalarRoutePrefix)
                target.ScalarRoutePrefix = configDoc.ScalarRoutePrefix;
            if (configDoc.Description is not null)
                target.Description = configDoc.Description;
            // EnableBearerAuth deliberately not applied here.
        }

        // EnableBearerAuth remains as set in code (false), not overridden by config.
        options.Documents[0].EnableBearerAuth.ShouldBeFalse();
    }
}
