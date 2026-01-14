using System.Text;
using System.Text.Json;

namespace PerformanceEngine.Evaluation.Domain.Tests.Determinism;

/// <summary>
/// Base class for determinism testing.
/// Verifies that operations produce byte-identical results across multiple executions.
/// </summary>
public abstract class DeterminismTestBase
{
    /// <summary>
    /// Default number of iterations for determinism verification
    /// </summary>
    protected const int DefaultIterations = 1000;

    /// <summary>
    /// Runs an operation multiple times and verifies all results are byte-identical.
    /// </summary>
    /// <typeparam name="T">Type of result to verify</typeparam>
    /// <param name="operation">Operation to execute repeatedly</param>
    /// <param name="iterations">Number of times to execute (default: 1000)</param>
    /// <returns>True if all results are byte-identical</returns>
    protected bool VerifyDeterminism<T>(Func<T> operation, int iterations = DefaultIterations)
    {
        if (iterations < 2)
        {
            throw new ArgumentException("Iterations must be at least 2", nameof(iterations));
        }

        var results = new List<string>(iterations);

        // Execute operation multiple times
        for (int i = 0; i < iterations; i++)
        {
            var result = operation();
            var serialized = SerializeToJson(result);
            results.Add(serialized);
        }

        // Verify all results are identical
        var first = results[0];
        return results.All(r => r == first);
    }

    /// <summary>
    /// Runs an operation multiple times with different input orders and verifies results are deterministic.
    /// </summary>
    /// <typeparam name="TInput">Type of input collection</typeparam>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="operation">Operation that takes input and produces result</param>
    /// <param name="inputs">Input collection to permute</param>
    /// <param name="permutations">Number of different input orderings to try</param>
    /// <returns>True if all permutations produce the same result</returns>
    protected bool VerifyOrderIndependence<TInput, TResult>(
        Func<IEnumerable<TInput>, TResult> operation,
        IEnumerable<TInput> inputs,
        int permutations = 10)
    {
        var inputList = inputs.ToList();
        if (inputList.Count < 2)
        {
            throw new ArgumentException("Need at least 2 inputs to test order independence", nameof(inputs));
        }

        var results = new List<string>(permutations);

        for (int i = 0; i < permutations; i++)
        {
            // Shuffle inputs
            var shuffled = ShuffleList(inputList, seed: i);
            
            // Execute operation
            var result = operation(shuffled);
            var serialized = SerializeToJson(result);
            results.Add(serialized);
        }

        // All results should be identical despite different input orders
        var first = results[0];
        return results.All(r => r == first);
    }

    /// <summary>
    /// Verifies that serialization is deterministic (same object serializes to same bytes)
    /// </summary>
    protected bool VerifySerializationDeterminism<T>(T obj, int iterations = DefaultIterations)
    {
        var serializations = new List<string>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            var serialized = SerializeToJson(obj);
            serializations.Add(serialized);
        }

        var first = serializations[0];
        return serializations.All(s => s == first);
    }

    /// <summary>
    /// Serializes an object to JSON with deterministic settings.
    /// </summary>
    private string SerializeToJson<T>(T obj)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Shuffles a list using a seeded random number generator for reproducibility.
    /// </summary>
    private List<T> ShuffleList<T>(List<T> list, int seed)
    {
        var rng = new Random(seed);
        var shuffled = new List<T>(list);
        
        int n = shuffled.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (shuffled[k], shuffled[n]) = (shuffled[n], shuffled[k]);
        }
        
        return shuffled;
    }

    /// <summary>
    /// Compares two byte arrays for equality.
    /// </summary>
    protected bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Computes SHA256 hash of a string for deterministic comparison.
    /// </summary>
    protected byte[] ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
    }
}
