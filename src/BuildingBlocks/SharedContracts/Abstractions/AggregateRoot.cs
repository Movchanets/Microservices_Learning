using System.Collections.ObjectModel;

namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Base class for aggregate roots. Collects domain events for dispatch after persistence.
/// </summary>
public abstract class AggregateRoot : Entity
{
	private readonly List<IDomainEvent> _domainEvents = [];
	private readonly ReadOnlyCollection<IDomainEvent> _readonlyDomainEvents;

	/// <summary>
	/// Initializes a new instance of the <see cref="AggregateRoot"/> class.
	/// Rationale: We initialize the ReadOnlyCollection wrapper here once during construction.
	/// This avoids allocating a new collection wrapper every time the DomainEvents property is accessed,
	/// satisfying the performance preference to avoid repeated .AsReadOnly() calls.
	/// </summary>
	protected AggregateRoot()
	{
		_readonlyDomainEvents = _domainEvents.AsReadOnly();
	}

	/// <summary>
	/// Gets the list of domain events that occurred within this aggregate.
	/// Rationale: Exposed as IReadOnlyList to prevent external modification of the aggregate's event list,
	/// ensuring all events are strictly controlled and added via <see cref="AddDomainEvent"/>.
	/// </summary>
	public IReadOnlyList<IDomainEvent> DomainEvents => _readonlyDomainEvents;

	/// <summary>
	/// Adds a domain event to the aggregate's internal collection.
	/// </summary>
	/// <param name="domainEvent">The domain event to record.</param>
	protected void AddDomainEvent(IDomainEvent domainEvent) =>
		_domainEvents.Add(domainEvent);

	/// <summary>
	/// Clears all recorded domain events.
	/// Rationale: Called typically after events have been dispatched to a message broker or MediatR,
	/// so they are not re-published if the aggregate root instance remains in memory.
	/// </summary>
	public void ClearDomainEvents() => _domainEvents.Clear();
}
