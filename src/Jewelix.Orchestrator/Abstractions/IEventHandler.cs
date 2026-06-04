namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Handles a domain event of type <typeparamref name="TEvent"/>.
/// Multiple handlers for the same event type are all invoked when the event is published.
/// </summary>
public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    /// <summary>Reacts to the published event.</summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
