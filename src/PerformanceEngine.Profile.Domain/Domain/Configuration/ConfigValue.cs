namespace PerformanceEngine.Profile.Domain.Domain.Configuration;

/// <summary>
/// Immutable value object representing a configuration value with its type.
/// </summary>
public sealed record ConfigValue
{
    public object Value { get; }
    public ConfigType Type { get; }

    public ConfigValue(object value, ConfigType type)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Type = type;

        // Validate type matches value
        ValidateTypeMatch(value, type);
    }

    /// <summary>
    /// Creates a ConfigValue by inferring the type from the value.
    /// </summary>
    public static ConfigValue Create(object value)
    {
        var type = ConfigTypeExtensions.ToConfigType(value);
        return new ConfigValue(value, type);
    }

    private static void ValidateTypeMatch(object value, ConfigType type)
    {
        var expectedType = ConfigTypeExtensions.ToConfigType(value);
        if (expectedType != type)
        {
            throw new ArgumentException(
                $"Value type {value.GetType()} does not match specified ConfigType {type}");
        }
    }

    public override string ToString() => $"{Value} ({Type})";
}
