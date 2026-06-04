namespace Jewelix.Orchestrator.Abstractions;

/// <summary>
/// Marker interface for a domain event (notification).
/// Multiple <see cref="IEventHandler{TEvent}"/> implementations may subscribe;
/// all are invoked in parallel when the event is published via
/// <see cref="IOrchestrator.PublishAsync{TEvent}"/>.
/// </summary>
public interface IEvent { }
