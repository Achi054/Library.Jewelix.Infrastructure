using Jewelix.Orchestrator.Tests.Fakes;

namespace Jewelix.Orchestrator.Tests;

public class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddJewelixOrchestrator_RegistersIOrchestrator()
    {
        using var provider = BuildProvider(s => s.AddJewelixOrchestrator());

        var orchestrator = provider.GetService<IOrchestrator>();

        orchestrator.ShouldNotBeNull();
        orchestrator.ShouldBeOfType<Orchestrator>();
    }

    [Fact]
    public void AddJewelixOrchestrator_IsMediatorSingleton()
    {
        using var provider = BuildProvider(s => s.AddJewelixOrchestrator());

        var first = provider.GetRequiredService<IOrchestrator>();
        var second = provider.GetRequiredService<IOrchestrator>();

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void AddJewelixOrchestrator_ScansAndRegistersCommandHandler()
    {
        using var provider = BuildProvider(s =>
            s.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly));

        var handler = provider.GetService<ICommandHandler<CreateOrderCommand, int>>();

        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<CreateOrderHandler>();
    }

    [Fact]
    public void AddJewelixOrchestrator_ScansAndRegistersQueryHandler()
    {
        using var provider = BuildProvider(s =>
            s.AddJewelixOrchestrator(typeof(GetProductHandler).Assembly));

        var handler = provider.GetService<IQueryHandler<GetProductQuery, string?>>();

        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<GetProductHandler>();
    }

    [Fact]
    public void AddJewelixOrchestrator_ScansAndRegistersEventHandlers()
    {
        using var provider = BuildProvider(s =>
            s.AddJewelixOrchestrator(typeof(OrderCreatedHandlerA).Assembly));

        var handlers = provider.GetServices<IEventHandler<OrderCreatedEvent>>().ToList();

        handlers.Count.ShouldBeGreaterThanOrEqualTo(2);
        handlers.ShouldContain(h => h is OrderCreatedHandlerA);
        handlers.ShouldContain(h => h is OrderCreatedHandlerB);
    }

    [Fact]
    public void AddJewelixOrchestrator_HandlerIsTransient()
    {
        using var provider = BuildProvider(s =>
            s.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly));

        var first = provider.GetRequiredService<ICommandHandler<CreateOrderCommand, int>>();
        var second = provider.GetRequiredService<ICommandHandler<CreateOrderCommand, int>>();

        first.ShouldNotBeSameAs(second);
    }

    [Fact]
    public void AddJewelixOrchestrator_WithNoAssemblies_OnlyRegistersMediatorWithoutThrowing()
    {
        var act = () => BuildProvider(s => s.AddJewelixOrchestrator());

        act.ShouldNotThrow();
    }

    [Fact]
    public void AddJewelixOrchestratorBehavior_RegistersOpenGenericBehavior()
    {
        using var provider = BuildProvider(s =>
        {
            s.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly);
            s.AddJewelixOrchestratorBehavior(typeof(CallTrackingBehavior<,>));
        });

        var behaviors = provider.GetServices<IPipelineBehavior<CreateOrderCommand, int>>().ToList();

        behaviors.ShouldNotBeEmpty();
        behaviors.ShouldContain(b => b is CallTrackingBehavior<CreateOrderCommand, int>);
    }

    [Fact]
    public void AddJewelixOrchestrator_ThrowsOnNullServices()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() => services.AddJewelixOrchestrator());
    }
}
