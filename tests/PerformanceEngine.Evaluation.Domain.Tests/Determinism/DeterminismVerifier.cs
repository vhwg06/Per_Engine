namespace PerformanceEngine.Evaluation.Domain.Tests.Determinism;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Utility for verifying deterministic behavior of Evaluation Domain entities.
/// Determinism means: identical inputs always produce identical outputs across multiple iterations.
/// </summary>
public static class DeterminismVerifier
{
    /// <summary>
    /// Default number of iterations for determinism verification.
    /// Higher iteration count = more confidence in determinism.
    /// </summary>
    public const int DefaultIterationCount = 1000;

    /// <summary>
    /// JSON serialization options configured for deterministic output.
    /// </summary>
    private static readonly JsonSerializerOptions DeterministicJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        }
    };

    /// <summary>
    /// Verifies that a factory function produces identical output across N iterations.
    /// All outputs are serialized to JSON and byte-compared for exact equality.
    /// </summary>
    /// <typeparam name="T">Type of object being created</typeparam>
    /// <param name="factory">Function that creates the object</param>
    /// <param name="iterationCount">Number of iterations to verify (default: 1000)</param>
    /// <returns>True if all iterations produced identical JSON serialization</returns>
    public static bool VerifyDeterministic<T>(
        Func<T> factory,
        int iterationCount = DefaultIterationCount)
        where T : notnull
    {
        var results = new List<string>(iterationCount);

        // Create objects and serialize to JSON
        for (int i = 0; i < iterationCount; i++)
        {
            var obj = factory();
            var json = JsonSerializer.Serialize(obj, DeterministicJsonOptions);
            results.Add(json);
        }

        // Verify all serializations are identical
        if (results.Count == 0)
            return false;

        var firstJson = results[0];
        return results.All(json => json == firstJson);
    }

    /// <summary>
    /// Verifies determinism and throws AssertionException if not deterministic.
    /// </summary>
    /// <typeparam name="T">Type of object being created</typeparam>
    /// <param name="factory">Function that creates the object</param>
    /// <param name="iterationCount">Number of iterations to verify (default: 1000)</param>
    public static void AssertDeterministic<T>(
        Func<T> factory,
        int iterationCount = DefaultIterationCount)
        where T : notnull
    {
        if (!VerifyDeterministic(factory, iterationCount))
        {
            throw new InvalidOperationException(
                $"Determinism verification failed: factory produced different outputs after {iterationCount} iterations");
        }
    }

    /// <summary>
    /// Returns representative JSON sample from N iterations.
    /// Useful for debugging determinism issues.
    /// </summary>
    /// <typeparam name="T">Type of object being created</typeparam>
    /// <param name="factory">Function that creates the object</param>
    /// <param name="iterationCount">Number of iterations to sample</param>
    /// <returns>List of (iteration index, JSON string) tuples</returns>
    public static IReadOnlyList<(int Index, string Json)> GetDeterminismSamples<T>(
        Func<T> factory,
        int iterationCount = 10)
        where T : notnull
    {
        var samples = new List<(int, string)>();

        for (int i = 0; i < iterationCount; i++)
        {
            var obj = factory();
            var json = JsonSerializer.Serialize(obj, DeterministicJsonOptions);
            samples.Add((i, json));
        }

        return samples;
    }

    /// <summary>
    /// Gets the canonical JSON representation of an object.
    /// </summary>
    /// <typeparam name="T">Type of object</typeparam>
    /// <param name="obj">Object to serialize</param>
    /// <returns>Deterministically formatted JSON string</returns>
    public static string ToCanonicalJson<T>(T obj)
        where T : notnull
    {
        return JsonSerializer.Serialize(obj, DeterministicJsonOptions);
    }

    /// <summary>
    /// Computes a simple hash of the JSON serialization for quick equality checks.
    /// Note: Hash collisions theoretically possible but extremely unlikely for distinct JSON.
    /// </summary>
    /// <typeparam name="T">Type of object</typeparam>
    /// <param name="obj">Object to hash</param>
    /// <returns>Hash code of canonical JSON representation</returns>
    public static int GetJsonHash<T>(T obj)
        where T : notnull
    {
        var json = ToCanonicalJson(obj);
        return json.GetHashCode();
    }
}
