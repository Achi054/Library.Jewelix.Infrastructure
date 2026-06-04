namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Handles <typeparamref name="TQuery"/> and returns <typeparamref name="TResult"/>.
/// Register implementations via <see cref="OrchestratorExtensions.AddJewelixOrchestrator"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Executes the query and returns its result.</summary>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
