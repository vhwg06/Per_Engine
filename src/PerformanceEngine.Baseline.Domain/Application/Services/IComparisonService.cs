namespace PerformanceEngine.Baseline.Domain.Application.Services;

using PerformanceEngine.Baseline.Domain.Application.Dto;
using PerformanceEngine.Baseline.Domain.Domain.Baselines;

/// <summary>
/// Application facade service for baseline and comparison operations.
/// Provides a simplified interface for infrastructure/presentation layers to interact with the baseline domain.
/// </summary>
public interface IComparisonService
{
    /// <summary>
    /// Creates a new baseline from the provided DTOs.
    /// </summary>
    /// <param name="metrics">The metric DTOs to include in the baseline.</param>
    /// <param name="tolerances">The tolerance DTOs for metric comparisons.</param>
    /// <returns>The DTO representing the created baseline.</returns>
    Task<BaselineDto> CreateBaselineAsync(
        IEnumerable<MetricDto> metrics,
        IEnumerable<ToleranceDto> tolerances);

    /// <summary>
    /// Performs a comparison between a baseline and current metrics.
    /// </summary>
    /// <param name="request">The comparison request containing baseline ID and current metrics.</param>
    /// <returns>The DTO representing the comparison results.</returns>
    Task<ComparisonResultDto> CompareAsync(ComparisonRequestDto request);

    /// <summary>
    /// Retrieves an existing baseline by ID.
    /// </summary>
    /// <param name="baselineId">The ID of the baseline to retrieve.</param>
    /// <returns>The DTO representing the baseline, or null if not found.</returns>
    Task<BaselineDto?> GetBaselineAsync(string baselineId);

    /// <summary>
    /// Checks if a baseline exists.
    /// </summary>
    /// <param name="baselineId">The ID of the baseline to check.</param>
    /// <returns>True if the baseline exists; false otherwise.</returns>
    Task<bool> BaselineExistsAsync(string baselineId);

    /// <summary>
    /// Deletes a baseline.
    /// </summary>
    /// <param name="baselineId">The ID of the baseline to delete.</param>
    Task DeleteBaselineAsync(string baselineId);
}
