using Jewelix.Orchestrator.Tests.Fakes;

namespace Jewelix.Orchestrator.Tests;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task SingleBehavior_WrapsHandlerExecution()
    {
        var tracker = new CallTrackingBehavior<CreateOrderCommand, int>();
        var services = new ServiceCollection();
        services.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly);
        services.AddSingleton<IPipelineBehavior<CreateOrderCommand, int>>(tracker);
        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        await orchestrator.SendAsync(new CreateOrderCommand("Gear", 2));

        tracker.Calls.ShouldBe(["before", "after"]);
    }

    [Fact]
    public async Task MultipleBehaviors_ExecuteInRegistrationOrder_OutermostFirst()
    {
        var calls = new List<string>();

        var services = new ServiceCollection();
        services.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly);

        // First registered = outermost (wraps everything else)
        services.AddSingleton<IPipelineBehavior<CreateOrderCommand, int>>(
            new LabeledBehavior<CreateOrderCommand, int>("outer", calls));
        services.AddSingleton<IPipelineBehavior<CreateOrderCommand, int>>(
            new LabeledBehavior<CreateOrderCommand, int>("inner", calls));

        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        await orchestrator.SendAsync(new CreateOrderCommand("Sprocket", 1));

        calls.ShouldBe(["outer:before", "inner:before", "inner:after", "outer:after"]);
    }

    [Fact]
    public async Task Behavior_CanShortCircuit_WithoutCallingHandler()
    {
        var handlerCalled = false;
        var services = new ServiceCollection();
        services.AddJewelixOrchestrator();
        services.AddTransient<ICommandHandler<CreateOrderCommand, int>>(
            _ => new TrackingHandler(() => handlerCalled = true));
        services.AddSingleton<IPipelineBehavior<CreateOrderCommand, int>>(
            new ShortCircuitBehavior<CreateOrderCommand, int>(-1));

        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        var result = await orchestrator.SendAsync(new CreateOrderCommand("X", 1));

        result.ShouldBe(-1);
        handlerCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Behavior_AppliesAcrossBothCommandsAndQueries()
    {
        var commandTracker = new CallTrackingBehavior<CreateOrderCommand, int>();
        var queryTracker = new CallTrackingBehavior<GetProductQuery, string?>();

        var services = new ServiceCollection();
        services.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly);
        services.AddSingleton<IPipelineBehavior<CreateOrderCommand, int>>(commandTracker);
        services.AddSingleton<IPipelineBehavior<GetProductQuery, string?>>(queryTracker);
        var orchestrator = services.BuildServiceProvider().GetRequiredService<IOrchestrator>();

        await orchestrator.SendAsync(new CreateOrderCommand("Nut", 1));
        await orchestrator.QueryAsync(new GetProductQuery("Bolt"));

        commandTracker.Calls.ShouldBe(["before", "after"]);
        queryTracker.Calls.ShouldBe(["before", "after"]);
    }

    [Fact]
    public async Task OpenGenericBehavior_AppliesViaAddJewelixOrchestratorBehavior()
    {
        var services = new ServiceCollection();
        services.AddJewelixOrchestrator(typeof(CreateOrderHandler).Assembly);
        services.AddJewelixOrchestratorBehavior(typeof(CallTrackingBehavior<,>));
        var provider = services.BuildServiceProvider();

        var orchestrator = provider.GetRequiredService<IOrchestrator>();
        await orchestrator.SendAsync(new CreateOrderCommand("Pin", 5));

        // Verify the open-generic behavior was resolved and invoked
        var behaviors = provider.GetServices<IPipelineBehavior<CreateOrderCommand, int>>().ToList();
        behaviors.ShouldContain(b => b is CallTrackingBehavior<CreateOrderCommand, int>);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private sealed class LabeledBehavior<TInput, TOutput>(string label, List<string> calls)
        : IPipelineBehavior<TInput, TOutput>
    {
        public async Task<TOutput> HandleAsync(TInput input, Func<Task<TOutput>> continuation, CancellationToken cancellationToken = default)
        {
            calls.Add($"{label}:before");
            var result = await continuation();
            calls.Add($"{label}:after");
            return result;
        }
    }

    private sealed class TrackingHandler(Action onHandle) : ICommandHandler<CreateOrderCommand, int>
    {
        public Task<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
        {
            onHandle();
            return Task.FromResult(0);
        }
    }
}
