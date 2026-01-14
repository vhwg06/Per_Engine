using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PerformanceEngine.Profile.Domain.Tests.Determinism;

/// <summary>
/// Base class for determinism tests. Provides infrastructure to run operations
/// multiple times and verify byte-identical results.
/// </summary>
public abstract class DeterminismTestBase
{
    private const int DefaultIterations = 1000;

    /// <summary>
    /// Runs an operation multiple times and verifies all results are byte-identical.
    /// </summary>
    /// <typeparam name="T">Type of result to verify</typeparam>
    /// <param name="operation">Operation to execute repeatedly</param>
    /// <param name="iterations">Number of iterations (default: 1000)</param>
    /// <param name="resultDescription">Description of the result for error messages</param>
    protected void AssertDeterministic<T>(
        Func<T> operation,
        int iterations = DefaultIterations,
        string resultDescription = "result")
    {
        var hashes = new HashSet<string>();
        var results = new List<string>();

        for (int i = 0; i < iterations; i++)
        {
            var result = operation();
            var serialized = SerializeResult(result);
            var hash = ComputeHash(serialized);
            
            hashes.Add(hash);
            
            // Store first result for comparison
            if (i == 0)
            {
                results.Add(serialized);
            }
        }

        // All hashes should be identical
        if (hashes.Count != 1)
        {
            throw new InvalidOperationException(
                $"Non-deterministic behavior detected for {resultDescription}. " +
                $"Found {hashes.Count} different results across {iterations} iterations. " +
                $"First result hash: {hashes.First()}");
        }
    }

    /// <summary>
    /// Runs an operation with different input orderings and verifies order-independent determinism.
    /// </summary>
    /// <typeparam name="TInput">Type of input collection</typeparam>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="inputs">Collection of inputs to reorder</param>
    /// <param name="operation">Operation that processes inputs</param>
    /// <param name="permutations">Number of random permutations to test</param>
    /// <param name="resultDescription">Description for error messages</param>
    protected void AssertOrderIndependentDeterminism<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<IEnumerable<TInput>, TResult> operation,
        int permutations = 100,
        string resultDescription = "result")
    {
        var inputList = inputs.ToList();
        var hashes = new HashSet<string>();
        var random = new Random(42); // Fixed seed for reproducibility

        // Test original order
        var originalResult = operation(inputList);
        var originalSerialized = SerializeResult(originalResult);
        var originalHash = ComputeHash(originalSerialized);
        hashes.Add(originalHash);

        // Test random permutations
        for (int i = 0; i < permutations; i++)
        {
            var shuffled = inputList.OrderBy(_ => random.Next()).ToList();
            var result = operation(shuffled);
            var serialized = SerializeResult(result);
            var hash = ComputeHash(serialized);
            hashes.Add(hash);
        }

        if (hashes.Count != 1)
        {
            throw new InvalidOperationException(
                $"Order-dependent behavior detected for {resultDescription}. " +
                $"Found {hashes.Count} different results across {permutations + 1} orderings. " +
                $"Expected order-independent determinism.");
        }
    }

    /// <summary>
    /// Verifies that two results are byte-identical when serialized.
    /// </summary>
    protected void AssertByteIdentical<T>(T result1, T result2, string description = "results")
    {
        var serialized1 = SerializeResult(result1);
        var serialized2 = SerializeResult(result2);
        var hash1 = ComputeHash(serialized1);
        var hash2 = ComputeHash(serialized2);

        if (hash1 != hash2)
        {
            throw new InvalidOperationException(
                $"Results are not byte-identical for {description}. " +
                $"Hash1: {hash1}, Hash2: {hash2}");
        }
    }

    /// <summary>
    /// Serializes a result to JSON string for comparison.
    /// </summary>
    private string SerializeResult<T>(T result)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        // For ResolvedProfile, serialize to DTO first
        if (result is ResolvedProfile resolved)
        {
            var dto = new
            {
                configuration = resolved.Configuration
                    .ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value.Value),
                auditTrail = resolved.AuditTrail
                    .ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value.Select(s => s.Type).ToList())
            };
            return JsonSerializer.Serialize(dto, options);
        }
        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// Computes SHA256 hash of a string.
    /// </summary>
    private string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Creates a fixed timestamp for testing (removes DateTime.Now non-determinism).
    /// </summary>
    protected DateTime GetFixedTimestamp() => new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
