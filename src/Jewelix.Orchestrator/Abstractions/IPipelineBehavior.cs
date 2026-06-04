namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Cross-cutting concern that wraps command and query handler execution.
/// Behaviors are resolved from DI and composed in registration order (first registered = outermost).
/// </summary>
/// <typeparam name="TInput">The command or query type.</typeparam>
/// <typeparam name="TOutput">The result type.</typeparam>
public interface IPipelineBehavior<in TInput, TOutput>
{
    /// <summary>
    /// Executes the behavior. Call <paramref name="continuation"/> to invoke the inner handler or behavior.
    /// </summary>
    Task<TOutput> HandleAsync(TInput input, Func<Task<TOutput>> continuation, CancellationToken cancellationToken = default);
}
