# Baseline Infrastructure - Guide

## Overview

This infrastructure layer provides Redis-based persistence for the Baseline Domain. It implements the `IBaselineRepository` port defined by the domain layer and manages all storage concerns.

## Redis Setup & Configuration

### Connection Configuration

Configure Redis connection via `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "BaselineTtl": "24:00:00"
  }
}
```

**Configuration Options:**
- `ConnectionString`: Redis server endpoint (default: `localhost:6379`)
- `BaselineTtl`: Time-to-live for baseline entries (default: 24 hours)

### Dependency Injection

Register infrastructure services in your application startup:

```csharp
using PerformanceEngine.Baseline.Infrastructure.Configuration;

services.AddBaselineInfrastructure(configuration);
```

This registers:
- `IConnectionMultiplexer` (singleton, connection pooling)
- `IBaselineRepository` → `RedisBaselineRepository`

## Connection Pooling Details

### RedisConnectionFactory

Manages connection multiplexer lifecycle:

```csharp
public class RedisConnectionFactory
{
    // Singleton connection multiplexer (thread-safe)
    private static readonly Lazy<IConnectionMultiplexer> _connection;
    
    public IConnectionMultiplexer GetConnection();
}
```

**Benefits:**
- **Thread-safe**: Single multiplexer shared across all threads
- **Efficient**: Reuses connections via internal pooling
- **Resilient**: Automatic reconnection on connection loss

**Best Practices:**
- Never dispose the connection multiplexer manually
- Let StackExchange.Redis manage connection lifecycle
- Connection is created lazily on first use

## TTL & Eviction Policy

### Time-to-Live (TTL)

Baselines expire automatically after TTL period:

- **Default**: 24 hours
- **Configurable**: Via `Redis:BaselineTtl` setting
- **Automatic**: Set on baseline creation, managed by Redis

### Key Expiration

```csharp
// Baseline keys use TTL
await db.StringSetAsync(
    key: RedisKeyBuilder.BaselineKey(baselineId),
    value: json,
    expiry: TimeSpan.FromHours(24)
);
```

**When baseline expires:**
- `GetByIdAsync` returns `null`
- No cleanup needed (Redis handles eviction)
- Application handles missing baseline gracefully

### Eviction Policy

Redis should be configured with appropriate eviction policy:

**Recommended**: `volatile-lru`
- Evicts least-recently-used keys with TTL set
- Protects non-expiring data
- Balances memory usage

**Configuration** (redis.conf):
```
maxmemory 1gb
maxmemory-policy volatile-lru
```

## Repository Implementation

### RedisBaselineRepository

Implements `IBaselineRepository` port:

```csharp
public class RedisBaselineRepository : IBaselineRepository
{
    Task<BaselineId> CreateAsync(Baseline baseline);
    Task<Baseline?> GetByIdAsync(BaselineId id);
    Task<IReadOnlyList<Baseline>> ListRecentAsync(int count);
}
```

**Storage Format:**
- JSON serialization via `BaselineRedisMapper`
- Stored as Redis strings
- Keys follow `baseline:{id}` pattern

### Key Naming Convention

`RedisKeyBuilder` generates consistent keys:

```csharp
// Baseline storage
baseline:{guid}

// Example
baseline:a3f7c9d2-4b8e-4a1f-9c3e-7d2a5f8b1e3c
```

**Benefits:**
- Namespace isolation (prefix: `baseline:`)
- No key collisions
- Easy querying via pattern matching

## Serialization & Mapping

### BaselineRedisMapper

Handles Baseline ↔ JSON conversion:

```csharp
public class BaselineRedisMapper
{
    string Serialize(Baseline baseline);
    Baseline Deserialize(string json);
}
```

**Serialization Strategy:**
- Uses `System.Text.Json`
- Preserves all baseline properties
- Supports polymorphic `IMetric` types
- Round-trip fidelity guaranteed

**Example JSON:**
```json
{
  "id": "a3f7c9d2-4b8e-4a1f-9c3e-7d2a5f8b1e3c",
  "createdAt": "2026-01-15T10:30:00Z",
  "metrics": [
    {
      "metricType": "ResponseTime",
      "value": 150.5,
      "unit": "ms"
    }
  ],
  "toleranceConfig": {
    "tolerances": [
      {
        "metricName": "ResponseTime",
        "type": "Relative",
        "amount": 10.0
      }
    ]
  }
}
```

## Scaling Considerations

### Performance Characteristics

**Latency:**
- Create: <5ms (p95)
- Retrieve: <5ms (p95)
- Serialize/Deserialize: <5ms (p95)

**Throughput:**
- Target: 1000+ operations/sec
- Bottleneck: Network latency to Redis
- Optimization: Connection pooling, pipeline commands

### Horizontal Scaling

**Application Tier:**
- Stateless repository design
- Multiple instances share Redis connection
- No coordination required

**Redis Tier:**
- Single Redis instance sufficient for most workloads
- Redis Cluster for high availability
- Redis Sentinel for automatic failover

### High Availability

**Options:**

1. **Redis Sentinel** (Recommended):
   - Automatic failover
   - Connection string includes sentinels
   - Client automatically reconnects

2. **Redis Cluster**:
   - Data sharding across nodes
   - Higher availability and throughput
   - More complex setup

**Connection String** (Sentinel):
```
sentinel1:26379,sentinel2:26379,sentinel3:26379,serviceName=mymaster
```

## Error Handling

### RepositoryException

Thrown on Redis connection or operation failure:

```csharp
try 
{
    var baseline = await repository.GetByIdAsync(id);
}
catch (RepositoryException ex)
{
    // Log error and handle gracefully
    _logger.LogError(ex, "Failed to retrieve baseline {Id}", id);
    // Return default or retry
}
```

**Common Causes:**
- Redis server unavailable
- Network timeout
- Authentication failure
- Memory limit exceeded

**Mitigation:**
- Circuit breaker pattern
- Retry with exponential backoff
- Fallback to default behavior

## Monitoring & Observability

### Key Metrics to Track

1. **Connection Health:**
   - Active connections
   - Failed connection attempts
   - Reconnection frequency

2. **Operation Latency:**
   - Create baseline latency (p50, p95, p99)
   - Retrieve baseline latency
   - Serialization overhead

3. **Cache Effectiveness:**
   - Hit rate (successful retrievals)
   - Miss rate (expired or not found)
   - Eviction count

4. **Error Rates:**
   - RepositoryException frequency
   - Timeout errors
   - Connection errors

### Logging

Infrastructure logs at appropriate levels:

```csharp
// Information: Normal operations
_logger.LogInformation("Created baseline {Id} with TTL {Ttl}", id, ttl);

// Warning: Degraded performance
_logger.LogWarning("Redis latency high: {Latency}ms", latency);

// Error: Operation failures
_logger.LogError(ex, "Failed to store baseline {Id}", id);
```

## Testing

### Integration Tests

Use `RedisBaselineWorkflowTests` to verify:
- Create and retrieve baselines
- TTL expiration behavior
- Concurrent access handling
- Serialization round-trip fidelity

### Local Redis Setup

**Docker:**
```bash
docker run --name redis -d -p 6379:6379 redis:latest
```

**Docker Compose:**
```yaml
version: '3.8'
services:
  redis:
    image: redis:latest
    ports:
      - "6379:6379"
    command: redis-server --maxmemory 1gb --maxmemory-policy volatile-lru
```

## Troubleshooting

### Common Issues

**1. Connection Refused**
- Check Redis server is running: `redis-cli ping`
- Verify connection string in configuration
- Check firewall/network access

**2. Slow Performance**
- Monitor Redis memory usage: `INFO memory`
- Check network latency: `redis-cli --latency`
- Review eviction policy: `INFO stats`

**3. Baselines Not Expiring**
- Verify TTL is set: `TTL baseline:{id}`
- Check maxmemory settings
- Ensure eviction policy configured

**4. Serialization Errors**
- Check IMetric implementations are serializable
- Verify JSON compatibility
- Review error logs for details

## Best Practices

1. **Connection Management:**
   - Never dispose IConnectionMultiplexer
   - Use dependency injection
   - Share connection across requests

2. **TTL Configuration:**
   - Set based on workflow duration
   - Balance memory vs. availability
   - Monitor expiration patterns

3. **Error Handling:**
   - Always catch RepositoryException
   - Log with sufficient context
   - Implement retry/fallback logic

4. **Performance:**
   - Monitor latency metrics
   - Use pipeline for batch operations
   - Keep payloads small (trim unnecessary data)

5. **Security:**
   - Use Redis AUTH in production
   - Enable TLS for network encryption
   - Restrict Redis network access

## References

- **StackExchange.Redis Documentation**: https://stackexchange.github.io/StackExchange.Redis/
- **Redis Documentation**: https://redis.io/documentation
- **Baseline Domain Guide**: `../PerformanceEngine.Baseline.Domain/IMPLEMENTATION_GUIDE.md`
