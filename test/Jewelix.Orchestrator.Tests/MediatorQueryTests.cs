using Jewelix.Orchestrator.Tests.Fakes;

namespace Jewelix.Orchestrator.Tests;

public class MediatorQueryTests
{
    private static IOrchestrator BuildMediator(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddJewelixOrchestrator(typeof(GetProductHandler).Assembly);
        configure?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IOrchestrator>();
    }

    [Fact]
    public async Task QueryAsync_DispatchesQueryToHandler_AndReturnsResult()
    {
        var orchestrator = BuildMediator();

        var result = await orchestrator.QueryAsync(new GetProductQuery("Bolt"));

        result.ShouldBe("Product:Bolt");
    }

    [Fact]
    public async Task QueryAsync_ThrowsInvalidOperationException_WhenNoHandlerRegistered()
    {
        var orchestrator = new ServiceCollection()
            .AddJewelixOrchestrator()
            .BuildServiceProvider()
            .GetRequiredService<IOrchestrator>();

        await Should.ThrowAsync<InvalidOperationException>(
            () => orchestrator.QueryAsync(new GetProductQuery("X")));
    }

    [Fact]
    public async Task QueryAsync_CanReturnNull()
    {
        var services = new ServiceCollection();
        services.AddJewelixOrchestrator();
        services.AddTransient<IQueryHandler<GetProductQuery, string?>, NullReturningHandler>();
        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        var result = await orchestrator.QueryAsync(new GetProductQuery("missing"));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task QueryAsync_PassesCancellationTokenToHandler()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;

        var services = new ServiceCollection();
        services.AddJewelixOrchestrator();
        services.AddTransient<IQueryHandler<GetProductQuery, string?>>(
            _ => new CapturingHandler(token => captured = token));
        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        await orchestrator.QueryAsync(new GetProductQuery("Y"), cts.Token);

        captured.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task QueryAsync_ThrowsOnNullQuery()
    {
        var orchestrator = BuildMediator();

        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.QueryAsync<string?>(null!));
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private sealed class NullReturningHandler : IQueryHandler<GetProductQuery, string?>
    {
        public Task<string?> HandleAsync(GetProductQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class CapturingHandler(Action<CancellationToken> capture)
        : IQueryHandler<GetProductQuery, string?>
    {
        public Task<string?> HandleAsync(GetProductQuery query, CancellationToken cancellationToken = default)
        {
            capture(cancellationToken);
            return Task.FromResult<string?>(null);
        }
    }
}
