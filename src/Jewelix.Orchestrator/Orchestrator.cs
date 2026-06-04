using Jewelix.Orchestrator.Abstractions;

namespace Jewelix.Orchestrator;

/// <summary>
/// Default <see cref="IOrchestrator"/> implementation. Resolves handlers from the DI container
/// and composes registered <see cref="IPipelineBehavior{TInput,TOutput}"/> instances around them.
/// </summary>
/// <remarks>
/// Handler resolution is reflection-based but uses per-type <see cref="MethodInfo"/> caching
/// so the reflection cost is paid only on the first dispatch for each concrete input type.
/// </remarks>
public sealed class Orchestrator : IOrchestrator
{
    private readonly IServiceProvider _provider;

    // Cached closed-generic MethodInfo instances, keyed by (inputType, resultType).
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> _commandCache = new();
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> _queryCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> _eventCache = new();

    private static readonly MethodInfo SendCommandInternalMethod =
        typeof(Orchestrator).GetMethod(nameof(SendCommandInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo SendQueryInternalMethod =
        typeof(Orchestrator).GetMethod(nameof(SendQueryInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo PublishInternalMethod =
        typeof(Orchestrator).GetMethod(nameof(PublishInternal), BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Initialises a new orchestrator backed by <paramref name="provider"/>.</summary>
    public Orchestrator(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <inheritdoc />
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var resultType = typeof(TResult);
        var method = _commandCache.GetOrAdd(
            (commandType, resultType),
            k => SendCommandInternalMethod.MakeGenericMethod(k.Item1, k.Item2));

        try
        {
            return (Task<TResult>)method.Invoke(this, [command, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable — satisfies the compiler
        }
    }

    /// <inheritdoc />
    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var resultType = typeof(TResult);
        var method = _queryCache.GetOrAdd(
            (queryType, resultType),
            k => SendQueryInternalMethod.MakeGenericMethod(k.Item1, k.Item2));

        try
        {
            return (Task<TResult>)method.Invoke(this, [query, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        var method = _eventCache.GetOrAdd(
            eventType,
            t => PublishInternalMethod.MakeGenericMethod(t));

        try
        {
            return (Task)method.Invoke(this, [domainEvent, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    // ── Typed internal dispatch — called via cached reflection above ──────

    private Task<TResult> SendCommandInternal<TCommand, TResult>(
        TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResult>
    {
        var handler = _provider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        var behaviors = _provider.GetServices<IPipelineBehavior<TCommand, TResult>>().Reverse();

        Func<Task<TResult>> pipeline = () => handler.HandleAsync(command, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(command, next, cancellationToken);
        }

        return pipeline();
    }

    private Task<TResult> SendQueryInternal<TQuery, TResult>(
        TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<TResult>
    {
        var handler = _provider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        var behaviors = _provider.GetServices<IPipelineBehavior<TQuery, TResult>>().Reverse();

        Func<Task<TResult>> pipeline = () => handler.HandleAsync(query, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(query, next, cancellationToken);
        }

        return pipeline();
    }

    private Task PublishInternal<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var handlers = _provider.GetServices<IEventHandler<TEvent>>();
        var tasks = handlers.Select(h => h.HandleAsync(domainEvent, cancellationToken));
        return Task.WhenAll(tasks);
    }
}
