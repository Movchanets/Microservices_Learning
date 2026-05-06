using System.Collections.ObjectModel;

namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Base class for aggregate roots. Collects domain events for dispatch after persistence.
/// </summary>
public abstract class AggregateRoot : Entity
{
	private readonly List<IDomainEvent> _domainEvents = [];
	private readonly ReadOnlyCollection<IDomainEvent> _readonlyDomainEvents;

	protected AggregateRoot()
	{
		_readonlyDomainEvents = _domainEvents.AsReadOnly();
	}

	public IReadOnlyList<IDomainEvent> DomainEvents => _readonlyDomainEvents;

	protected void AddDomainEvent(IDomainEvent domainEvent) =>
		_domainEvents.Add(domainEvent);

	public void ClearDomainEvents() => _domainEvents.Clear();
}
