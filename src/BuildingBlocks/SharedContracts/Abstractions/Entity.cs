namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Base class for all entities with identity.
/// </summary>
public abstract class Entity
{
	public Guid Id { get; protected init; } = Guid.NewGuid();

	public override bool Equals(object? obj) =>
		obj is Entity other && Id == other.Id;

	public override int GetHashCode() => Id.GetHashCode();
}
