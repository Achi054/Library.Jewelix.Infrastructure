using Jewelix.Orchestrator.Tests.Fakes;

namespace Jewelix.Orchestrator.Tests;

public class MediatorCommandTests
{
    private static IOrchestrator BuildMediator(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly);
        configure?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IOrchestrator>();
    }

    [Fact]
    public async Task SendAsync_DispatchesCommandToHandler_AndReturnsResult()
    {
        var orchestrator = BuildMediator();

        var result = await orchestrator.SendAsync(new CreateOrderCommand("Widget", 3));

        result.ShouldBe(30); // 3 * 10
    }

    [Fact]
    public async Task SendAsync_ThrowsInvalidOperationException_WhenNoHandlerRegistered()
    {
        var orchestrator = new ServiceCollection()
            .AddJewelixOrchestrator() // no assemblies — no handlers
            .BuildServiceProvider()
            .GetRequiredService<IOrchestrator>();

        await Should.ThrowAsync<InvalidOperationException>(
            () => orchestrator.SendAsync(new CreateOrderCommand("Widget", 1)));
    }

    [Fact]
    public async Task SendAsync_PassesCancellationTokenToHandler()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;

        var services = new ServiceCollection();
        services.AddJewelixOrchestrator();
        services.AddTransient<ICommandHandler<CreateOrderCommand, int>>(
            _ => new CapturingHandler(token => captured = token));
        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        await orchestrator.SendAsync(new CreateOrderCommand("X", 1), cts.Token);

        captured.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task SendAsync_ThrowsOnNullCommand()
    {
        var orchestrator = BuildMediator();

        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.SendAsync<int>(null!));
    }

    // ── helper ────────────────────────────────────────────────────────────

    private sealed class CapturingHandler(Action<CancellationToken> capture)
        : ICommandHandler<CreateOrderCommand, int>
    {
        public Task<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
        {
            capture(cancellationToken);
            return Task.FromResult(0);
        }
    }
}
