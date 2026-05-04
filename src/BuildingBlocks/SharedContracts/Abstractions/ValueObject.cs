namespace BuildingBlocks.SharedContracts.Abstractions;

/// <summary>
/// Base class for value objects. Equality is determined by component values.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
	protected abstract IEnumerable<object?> GetEqualityComponents();

	public bool Equals(ValueObject? other)
	{
		if (other is null || GetType() != other.GetType())
			return false;

		return GetEqualityComponents()
			.SequenceEqual(other.GetEqualityComponents());
	}

	public override bool Equals(object? obj) =>
		obj is ValueObject other && Equals(other);

	public override int GetHashCode() =>
		GetEqualityComponents()
			.Aggregate(0, (hash, component) =>
				HashCode.Combine(hash, component?.GetHashCode() ?? 0));

	public static bool operator ==(ValueObject? left, ValueObject? right) =>
		Equals(left, right);

	public static bool operator !=(ValueObject? left, ValueObject? right) =>
		!Equals(left, right);
}
