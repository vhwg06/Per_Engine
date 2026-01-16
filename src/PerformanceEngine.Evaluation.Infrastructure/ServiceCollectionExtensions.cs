namespace PerformanceEngine.Evaluation.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using PerformanceEngine.Evaluation.Infrastructure.Persistence;
using PerformanceEngine.Evaluation.Ports;

/// <summary>
/// Service collection extensions for registering evaluation infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register in-memory evaluation result repository for development and testing.
    /// </summary>
    public static IServiceCollection AddInMemoryEvaluationResultRepository(
        this IServiceCollection services)
    {
        services.AddSingleton<IEvaluationResultRepository, InMemoryEvaluationResultRepository>();
        return services;
    }
}
