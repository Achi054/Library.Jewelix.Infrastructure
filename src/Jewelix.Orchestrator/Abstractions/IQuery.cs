namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Marker interface for a query that returns <typeparamref name="TResult"/>.
/// Implement on a record or class that represents a read-only data request.
/// </summary>
public interface IQuery<out TResult> { }
