namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Dispatches commands, queries, and domain events to their registered handlers.
/// Inject this interface at call sites; use <see cref="OrchestratorExtensions.AddJewelixOrchestrator"/> to register.
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// Sends <paramref name="command"/> to its single registered
    /// <see cref="ICommandHandler{TCommand,TResult}"/>, running it through
    /// any registered <see cref="IPipelineBehavior{TInput,TOutput}"/> first.
    /// </summary>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends <paramref name="query"/> to its single registered
    /// <see cref="IQueryHandler{TQuery,TResult}"/>, running it through
    /// any registered <see cref="IPipelineBehavior{TInput,TOutput}"/> first.
    /// </summary>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes <paramref name="domainEvent"/> to all registered
    /// <see cref="IEventHandler{TEvent}"/> implementations in parallel.
    /// Completes when all handlers finish. If no handlers are registered the call is a no-op.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
