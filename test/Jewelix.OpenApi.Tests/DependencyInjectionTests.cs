using Microsoft.Extensions.DependencyInjection;

namespace Jewelix.OpenApi.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddJewelixOpenApi_WithNullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            OpenApiExtensions.AddJewelixOpenApi(null!));
    }

    [Fact]
    public void AddJewelixOpenApi_WithDefaultOptions_RegistersOptionsSingleton()
    {
        var services = new ServiceCollection();
        services.AddJewelixOpenApi();

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<JewelixOpenApiOptions>();

        options.ShouldNotBeNull();
        options.Documents.ShouldHaveSingleItem();
    }

    [Fact]
    public void AddJewelixOpenApi_WithConfigureDelegate_AppliesOptionsToSingleton()
    {
        var services = new ServiceCollection();
        services.AddJewelixOpenApi(opts =>
        {
            opts.Documents[0].Title = "Custom Title";
            opts.Documents[0].EnableBearerAuth = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<JewelixOpenApiOptions>();

        options.Documents[0].Title.ShouldBe("Custom Title");
        options.Documents[0].EnableBearerAuth.ShouldBeTrue();
    }

    [Fact]
    public void AddJewelixOpenApi_WithMultipleDocuments_AllDocumentsRegisteredInSingleton()
    {
        var services = new ServiceCollection();
        services.AddJewelixOpenApi(opts =>
        {
            opts.Documents =
            [
                new JewelixOpenApiDocument { Name = "v1", Title = "API v1" },
                new JewelixOpenApiDocument { Name = "v2", Title = "API v2" },
            ];
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<JewelixOpenApiOptions>();

        options.Documents.Count.ShouldBe(2);
        options.Documents[0].Name.ShouldBe("v1");
        options.Documents[1].Name.ShouldBe("v2");
    }

    [Fact]
    public void AddJewelixOpenApi_ReturnsServicesForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddJewelixOpenApi();
        result.ShouldBeSameAs(services);
    }
}
