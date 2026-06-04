namespace Jewelix.Orchestrator.Tests.Fakes;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateOrderCommand(string Product, int Quantity) : ICommand<int>;

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
{
    public Task<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(command.Quantity * 10);
}

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetProductQuery(string Name) : IQuery<string?>;

public class GetProductHandler : IQueryHandler<GetProductQuery, string?>
{
    public Task<string?> HandleAsync(GetProductQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>($"Product:{query.Name}");
}

// ── Events ────────────────────────────────────────────────────────────────────

public record OrderCreatedEvent(int OrderId) : IEvent;

public class OrderCreatedHandlerA : IEventHandler<OrderCreatedEvent>
{
    public List<int> Received { get; } = [];

    public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Received.Add(domainEvent.OrderId);
        return Task.CompletedTask;
    }
}

public class OrderCreatedHandlerB : IEventHandler<OrderCreatedEvent>
{
    public List<int> Received { get; } = [];

    public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Received.Add(domainEvent.OrderId);
        return Task.CompletedTask;
    }
}

// ── Pipeline behaviors ────────────────────────────────────────────────────────

public class CallTrackingBehavior<TInput, TOutput> : IPipelineBehavior<TInput, TOutput>
{
    public List<string> Calls { get; } = [];

    public async Task<TOutput> HandleAsync(TInput input, Func<Task<TOutput>> continuation, CancellationToken cancellationToken = default)
    {
        Calls.Add("before");
        var result = await continuation();
        Calls.Add("after");
        return result;
    }
}

public class ShortCircuitBehavior<TInput, TOutput> : IPipelineBehavior<TInput, TOutput>
{
    private readonly TOutput _shortCircuitResult;

    public ShortCircuitBehavior(TOutput shortCircuitResult) => _shortCircuitResult = shortCircuitResult;

    public Task<TOutput> HandleAsync(TInput input, Func<Task<TOutput>> continuation, CancellationToken cancellationToken = default)
        => Task.FromResult(_shortCircuitResult);
}
