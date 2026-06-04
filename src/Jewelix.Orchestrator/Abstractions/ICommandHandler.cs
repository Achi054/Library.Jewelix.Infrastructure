namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Handles <typeparamref name="TCommand"/> and returns <typeparamref name="TResult"/>.
/// Register implementations via <see cref="OrchestratorExtensions.AddJewelixOrchestrator"/>.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Executes the command and returns its result.</summary>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
