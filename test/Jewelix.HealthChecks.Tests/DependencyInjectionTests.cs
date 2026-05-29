using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewelix.HealthChecks.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddJewelixHealthChecks_WithNullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            HealthCheckExtensions.AddJewelixHealthChecks(null!));
    }

    [Fact]
    public void AddJewelixHealthChecks_WithDefaultOptions_RegistersOptionsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJewelixHealthChecks();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<JewelixHealthCheckOptions>();

        options.ShouldNotBeNull();
        options.ConnectionString.ShouldBeNull();
        options.Services.ShouldBeEmpty();
    }

    [Fact]
    public void AddJewelixHealthChecks_WithConfigureDelegate_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJewelixHealthChecks(configure: opts =>
        {
            opts.ConnectionString = "Server=localhost;Database=Test;";
            opts.Services =
            [
                new JewelixServiceCheck { Name = "api1", Uri = "https://example.com" },
            ];
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<JewelixHealthCheckOptions>();

        options.ConnectionString.ShouldBe("Server=localhost;Database=Test;");
        options.Services.Count.ShouldBe(1);
        options.Services[0].Name.ShouldBe("api1");
    }

    [Fact]
    public void AddJewelixHealthChecks_ReturnsServicesForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var returned = services.AddJewelixHealthChecks();
        returned.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddJewelixHealthChecks_ServiceWithBlankUri_DoesNotThrow()
    {
        // Blank/whitespace URIs from appsettings binding are silently skipped
        // rather than throwing a UriFormatException at startup.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJewelixHealthChecks(configure: opts =>
            opts.Services = [new JewelixServiceCheck { Name = "bad", Uri = "   " }]);

        var provider = services.BuildServiceProvider();
        provider.ShouldNotBeNull();
    }

    [Fact]
    public void AddJewelixHealthChecks_WithConfiguration_BindsConnectionString()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=cfg;Database=CfgDb;",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJewelixHealthChecks(configuration: config);
        var options = services.BuildServiceProvider()
                              .GetRequiredService<JewelixHealthCheckOptions>();

        options.ConnectionString.ShouldBe("Server=cfg;Database=CfgDb;");
    }

    [Fact]
    public void AddJewelixHealthChecks_WithConfiguration_BindsServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:0:Name"] = "Payment API",
                ["Services:0:Uri"]  = "https://payments.example.com",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJewelixHealthChecks(configuration: config);
        var options = services.BuildServiceProvider()
                              .GetRequiredService<JewelixHealthCheckOptions>();

        options.Services.Count.ShouldBe(1);
        options.Services[0].Name.ShouldBe("Payment API");
        options.Services[0].Uri.ShouldBe("https://payments.example.com");
    }

    [Fact]
    public void AddJewelixHealthChecks_WithConfigAndDelegate_ConfigWinsOverDelegate()
    {
        // Delegate sets "Server=delegate;", config sets "Server=config;" — config must win.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=config;Database=CfgDb;",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJewelixHealthChecks(
            configuration: config,
            configure: opts => opts.ConnectionString = "Server=delegate;Database=DelDb;");
        var options = services.BuildServiceProvider()
                              .GetRequiredService<JewelixHealthCheckOptions>();

        options.ConnectionString.ShouldBe("Server=config;Database=CfgDb;");
    }

    [Fact]
    public void AddJewelixHealthChecks_WithConfigEmptyServices_PreservesDelegateServices()
    {
        // Config has no Services entries — delegate list must be preserved.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=localhost;",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJewelixHealthChecks(
            configuration: config,
            configure: opts =>
                opts.Services = [new JewelixServiceCheck { Name = "api1", Uri = "https://example.com" }]);
        var options = services.BuildServiceProvider()
                              .GetRequiredService<JewelixHealthCheckOptions>();

        options.Services.Count.ShouldBe(1);
        options.Services[0].Name.ShouldBe("api1");
    }
}
