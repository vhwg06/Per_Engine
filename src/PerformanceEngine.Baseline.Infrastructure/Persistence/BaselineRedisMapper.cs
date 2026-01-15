namespace PerformanceEngine.Baseline.Infrastructure.Persistence;

using System.Diagnostics.CodeAnalysis;
using PerformanceEngine.Baseline.Domain.Application.Dto;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;
using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Mapper for serializing/deserializing baselines to/from JSON for Redis storage.
/// </summary>
public class BaselineRedisMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a baseline to JSON for storage.
    /// </summary>
    /// <param name="baseline">The baseline to serialize</param>
    /// <returns>JSON string representation</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "BaselineDto is a known type")]
    public static string Serialize(Baseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        var dto = BaselineDto.FromDomain(baseline);
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    /// <summary>
    /// Deserializes a baseline from JSON storage.
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>Reconstructed baseline</returns>
    /// <exception cref="InvalidOperationException">If deserialization fails</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "BaselineDto is a known type")]
    public static Baseline Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be empty.", nameof(json));

        try
        {
            var dto = JsonSerializer.Deserialize<BaselineDto>(json, JsonOptions);
            if (dto == null)
                throw new InvalidOperationException("Deserialization resulted in null baseline.");

            return dto.ToDomain();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to deserialize baseline from JSON.", ex);
        }
    }

    /// <summary>
    /// Verifies round-trip fidelity (serialize → deserialize produces equal baseline).
    /// </summary>
    /// <param name="baseline">The baseline to test</param>
    /// <returns>True if round-trip preserves equality</returns>
    public static bool VerifyRoundTripFidelity(Baseline baseline)
    {
        try
        {
            var json = Serialize(baseline);
            var deserialized = Deserialize(json);
            return baseline.Equals(deserialized);
        }
        catch
        {
            return false;
        }
    }
}
