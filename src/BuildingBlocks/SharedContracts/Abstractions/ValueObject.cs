namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Base class for value objects. Equality is determined by component values.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Yields the individual components that make up the value object for equality comparison.
    /// Rationale: Requires derived classes to explicitly declare which properties contribute
    /// to the object's equivalence, ensuring consistent equality checks.
    /// </summary>
    /// <returns>An enumerable of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Indicates whether the current object is equal to another value object.
    /// Rationale: Performs a sequence equality check on all properties yielded by <see cref="GetEqualityComponents"/>.
    /// </summary>
    /// <param name="other">A value object to compare with this object.</param>
    /// <returns>True if the current object is equal to the other parameter; otherwise, false.</returns>
    public bool Equals(ValueObject? other)
    {
        if (other is null || GetType() != other.GetType())
            return false;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current value object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is ValueObject other && Equals(other);

    /// <summary>
    /// Serves as the default hash function.
    /// Rationale: Combines the hash codes of all components returned by <see cref="GetEqualityComponents"/>
    /// so that two logically equivalent value objects produce the same hash code.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component?.GetHashCode() ?? 0));

    /// <summary>
    /// Implements the equality operator for value objects.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        Equals(left, right);

    /// <summary>
    /// Implements the inequality operator for value objects.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !Equals(left, right);
}
