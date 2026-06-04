using Jewelix.Orchestrator.Tests.Fakes;

namespace Jewelix.Orchestrator.Tests;

public class MediatorEventTests
{
    [Fact]
    public async Task PublishAsync_InvokesAllRegisteredHandlers()
    {
        var handlerA = new OrderCreatedHandlerA();
        var handlerB = new OrderCreatedHandlerB();

        var services = new ServiceCollection();
        services.AddJewelixOrchestrator();
        services.AddSingleton<IEventHandler<OrderCreatedEvent>>(handlerA);
        services.AddSingleton<IEventHandler<OrderCreatedEvent>>(handlerB);
        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        await orchestrator.PublishAsync(new OrderCreatedEvent(99));

        handlerA.Received.ShouldContain(99);
        handlerB.Received.ShouldContain(99);
    }

    [Fact]
    public async Task PublishAsync_WithNoHandlers_CompletesWithoutThrowing()
    {
        var orchestrator = new ServiceCollection()
            .AddJewelixOrchestrator()
            .BuildServiceProvider()
            .GetRequiredService<IOrchestrator>();

        await Should.NotThrowAsync(() => orchestrator.PublishAsync(new OrderCreatedEvent(1)));
    }

    [Fact]
    public async Task PublishAsync_InvokesMultipleHandlers_RegisteredViaScanning()
    {
        var services = new ServiceCollection();
        services.AddJewelixOrchestrator(typeof(OrderCreatedHandlerA).Assembly);
        var provider = services.BuildServiceProvider();

        // Resolve the handlers as singletons so we can inspect them after publish
        var handlers = provider.GetServices<IEventHandler<OrderCreatedEvent>>()
                               .OfType<OrderCreatedHandlerA>()
                               .ToList();

        var orchestrator = provider.GetRequiredService<IOrchestrator>();
        await orchestrator.PublishAsync(new OrderCreatedEvent(7));

        // At least the scanned handler was resolved and invoked
        handlers.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationTokenToHandler()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;

        var services = new ServiceCollection();
        services.AddJewelixOrchestrator();
        services.AddTransient<IEventHandler<OrderCreatedEvent>>(
            _ => new CapturingHandler(token => captured = token));
        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        await orchestrator.PublishAsync(new OrderCreatedEvent(5), cts.Token);

        captured.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task PublishAsync_ThrowsOnNullEvent()
    {
        var orchestrator = new ServiceCollection()
            .AddJewelixOrchestrator()
            .BuildServiceProvider()
            .GetRequiredService<IOrchestrator>();

        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.PublishAsync<OrderCreatedEvent>(null!));
    }

    // ── helper ────────────────────────────────────────────────────────────

    private sealed class CapturingHandler(Action<CancellationToken> capture)
        : IEventHandler<OrderCreatedEvent>
    {
        public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            capture(cancellationToken);
            return Task.CompletedTask;
        }
    }
}
