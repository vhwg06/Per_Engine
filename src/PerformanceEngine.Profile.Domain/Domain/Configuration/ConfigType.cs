namespace PerformanceEngine.Profile.Domain.Domain.Configuration;

/// <summary>
/// Represents the type of a configuration value.
/// </summary>
public enum ConfigType
{
    String,
    Int,
    Duration,
    Double,
    Bool
}

/// <summary>
/// Extension methods for ConfigType.
/// </summary>
public static class ConfigTypeExtensions
{
    /// <summary>
    /// Determines the ConfigType from a runtime value.
    /// </summary>
    public static ConfigType ToConfigType(object value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return value switch
        {
            string => ConfigType.String,
            int => ConfigType.Int,
            long => ConfigType.Int,
            TimeSpan => ConfigType.Duration,
            double => ConfigType.Double,
            float => ConfigType.Double,
            decimal => ConfigType.Double,
            bool => ConfigType.Bool,
            _ => throw new ArgumentException($"Unsupported config value type: {value.GetType()}")
        };
    }
}
