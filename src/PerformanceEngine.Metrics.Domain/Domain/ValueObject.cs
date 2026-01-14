namespace PerformanceEngine.Metrics.Domain;

/// <summary>
/// Abstract base class for all value objects in the domain.
/// Value objects are immutable by design and are compared by value, not by reference.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Gets the atomic values that make up this value object.
    /// Used for equality comparison.
    /// </summary>
    /// <returns>A collection of values that comprise this value object</returns>
    protected abstract IEnumerable<object?> GetAtomicValues();

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode()
    {
        return GetAtomicValues()
            .Aggregate(1, (current, value) =>
            {
                unchecked
                {
                    return current * 31 + (value?.GetHashCode() ?? 0);
                }
            });
    }

    public static bool operator ==(ValueObject? a, ValueObject? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(ValueObject? a, ValueObject? b)
    {
        return !(a == b);
    }
}
