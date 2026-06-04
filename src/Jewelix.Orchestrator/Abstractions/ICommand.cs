namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Marker interface for a command that produces <typeparamref name="TResult"/>.
/// Implement on a record or class that represents an intent to mutate state.
/// </summary>
public interface ICommand<out TResult> { }
