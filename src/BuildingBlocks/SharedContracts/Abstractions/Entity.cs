namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Base class for all entities with identity.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// Rationale: Generates a new Guid by default to ensure entities have a unique identity
    /// immediately upon instantiation, even before they are persisted to a database.
    /// </summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// Rationale: Entities are defined by their identity, not their attributes. Thus, equality
    /// is determined solely by comparing the <see cref="Id"/> property.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the specified object is an Entity and has the same Id; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Entity other && Id == other.Id;

    /// <summary>
    /// Serves as the default hash function.
    /// Rationale: Since equality is based on the <see cref="Id"/>, the hash code must also be based
    /// solely on the <see cref="Id"/> to adhere to equality constraints in collections (e.g. HashSets, Dictionaries).
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode() => Id.GetHashCode();
}
