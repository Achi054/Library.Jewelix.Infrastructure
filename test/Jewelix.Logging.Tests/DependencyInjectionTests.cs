using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jewelix.Logging.Tests;

/// <summary>
/// Tests for <see cref="LoggerExtensions.AddJewelixLogger"/>: DI registration of
/// <see cref="SerilogLogger{T}"/>, replacement of the default <c>ILogger&lt;&gt;</c>
/// registration, and null-argument guard behaviour.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddJewelixLogger_WhenCalledWithServices_ResolvesSerilogLoggerForOpenGeneric()
    {
        var services = new ServiceCollection();
        services.AddJewelixLogger();

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetService<ILogger<DependencyInjectionTests>>();

        logger.ShouldBeOfType<SerilogLogger<DependencyInjectionTests>>();
    }

    [Fact]
    public void AddJewelixLogger_WhenDefaultLoggerRegistered_ReplacesWithSerilogLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging(); // Pretends a host already registered the defaults.
        services.AddJewelixLogger();

        var registrations = services.Where(d => d.ServiceType == typeof(ILogger<>)).ToList();
        registrations.ShouldHaveSingleItem();
        registrations[0].ImplementationType.ShouldBe(typeof(SerilogLogger<>));
    }

    [Fact]
    public void AddJewelixLogger_WithNullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => LoggerExtensions.AddJewelixLogger(null!));
    }
}
