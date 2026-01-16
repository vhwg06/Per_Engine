namespace PerformanceEngine.Baseline.Infrastructure.Configuration;

using Microsoft.Extensions.DependencyInjection;
using PerformanceEngine.Baseline.Infrastructure.Persistence;

/// <summary>
/// Dependency injection extension methods for Baseline infrastructure setup.
/// </summary>
public static class BaselineInfrastructureExtensions
{
    /// <summary>
    /// Adds baseline infrastructure services to the dependency injection container.
    /// Configures Redis connection, repository, and serialization services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="redisConnectionString">Redis connection string (e.g., "localhost:6379")</param>
    /// <param name="baselineTtl">Optional TTL for baseline entries (default 24 hours)</param>
    /// <returns>The modified service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">If services or connectionString is null</exception>
    public static IServiceCollection AddBaselineInfrastructure(
        this IServiceCollection services,
        string redisConnectionString,
        TimeSpan? baselineTtl = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new ArgumentException("Redis connection string cannot be empty.", nameof(redisConnectionString));

        // Register Redis connection multiplexer
        var connectionOptions = ConfigurationOptions.Parse(redisConnectionString);
        connectionOptions.ConnectTimeout = 5000;
        connectionOptions.SyncTimeout = 5000;
        connectionOptions.AbortOnConnectFail = false;

        var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionOptions);

        services.AddSingleton(connectionMultiplexer);

        // Register Redis connection factory
        services.AddSingleton(sp => new RedisConnectionFactory(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            baselineTtl ?? TimeSpan.FromHours(24)
        ));

        // Register Redis baseline repository
        services.AddSingleton<IBaselineRepository, RedisBaselineRepository>();

        return services;
    }

    /// <summary>
    /// Adds baseline infrastructure services using configuration from options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure baseline infrastructure options</param>
    /// <returns>The modified service collection for chaining</returns>
    public static IServiceCollection AddBaselineInfrastructure(
        this IServiceCollection services,
        Action<BaselineInfrastructureOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        var options = new BaselineInfrastructureOptions();
        configureOptions(options);

        return services.AddBaselineInfrastructure(options.RedisConnectionString, options.BaselineTtl);
    }
}

/// <summary>
/// Configuration options for baseline infrastructure.
/// </summary>
public class BaselineInfrastructureOptions
{
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Gets or sets the TTL for baseline entries.
    /// </summary>
    public TimeSpan? BaselineTtl { get; set; } = TimeSpan.FromHours(24);
}
