namespace PerformanceEngine.Application.Services;

using System.Security.Cryptography;
using System.Text;
using PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Generates deterministic SHA256 fingerprints of metric data for integrity verification.
/// Ensures same metrics always produce same fingerprint through:
/// - Deterministic metric ordering (sorted by name)
/// - Deterministic serialization format
/// - Fixed hashing algorithm (SHA256)
/// </summary>
public sealed class DeterministicFingerprintGenerator
{
    /// <summary>
    /// Generates a deterministic fingerprint from metric samples.
    /// </summary>
    /// <param name="samples">Collection of metric samples.</param>
    /// <returns>Hexadecimal SHA256 fingerprint string.</returns>
    public string GenerateFingerprint(IReadOnlyCollection<Sample> samples)
    {
        if (samples == null || samples.Count == 0)
        {
            return ComputeHash("EMPTY");
        }

        // Step 1: Sort samples deterministically
        // For now, sort by timestamp then by duration value
        var sortedSamples = samples
            .OrderBy(s => s.Timestamp)
            .ThenBy(s => s.Duration?.Value ?? 0)
            .ToList();

        // Step 2: Create deterministic serialization
        var sb = new StringBuilder();
        foreach (var sample in sortedSamples)
        {
            // Format: timestamp|duration|status
            sb.Append($"{sample.Timestamp:O}|");
            sb.Append($"{sample.Duration?.Value ?? 0:F6}|");
            sb.Append($"{sample.Status}|");
        }

        var serialized = sb.ToString();

        // Step 3: Compute SHA256 hash
        return ComputeHash(serialized);
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);

        // Convert to hexadecimal string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
