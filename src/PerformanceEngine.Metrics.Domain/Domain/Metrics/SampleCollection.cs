namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// An immutable, thread-safe container for multiple samples.
/// This collection uses ImmutableList for lock-free concurrency.
/// </summary>
public sealed class SampleCollection
{
    private readonly ImmutableList<Sample> _samples;

    /// <summary>
    /// Gets the number of samples in this collection
    /// </summary>
    public int Count => _samples.Count;

    /// <summary>
    /// Gets whether this collection is empty
    /// </summary>
    public bool IsEmpty => _samples.IsEmpty;

    private SampleCollection(ImmutableList<Sample> samples)
    {
        _samples = samples;
    }

    /// <summary>
    /// Creates a new empty sample collection
    /// </summary>
    public static SampleCollection Empty => new(ImmutableList<Sample>.Empty);

    /// <summary>
    /// Creates a sample collection from an enumerable of samples
    /// </summary>
    public static SampleCollection Create(IEnumerable<Sample> samples)
    {
        return new SampleCollection(ImmutableList.CreateRange(samples ?? throw new ArgumentNullException(nameof(samples))));
    }

    /// <summary>
    /// Adds a sample to this collection, returning a new collection
    /// (functional pattern - original collection is unchanged)
    /// </summary>
    public SampleCollection Add(Sample sample)
    {
        if (sample == null)
        {
            throw new ArgumentNullException(nameof(sample));
        }

        return new SampleCollection(_samples.Add(sample));
    }

    /// <summary>
    /// Adds multiple samples to this collection, returning a new collection
    /// </summary>
    public SampleCollection AddRange(IEnumerable<Sample> samples)
    {
        if (samples == null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        return new SampleCollection(_samples.AddRange(samples));
    }

    /// <summary>
    /// Gets a snapshot of all samples at this moment
    /// </summary>
    public ImmutableList<Sample> GetSnapshot()
    {
        return _samples;
    }

    /// <summary>
    /// Gets an enumerable of all samples in insertion order
    /// </summary>
    public IEnumerable<Sample> AllSamples => _samples;

    /// <summary>
    /// Gets only the successful samples
    /// </summary>
    public IEnumerable<Sample> SuccessfulSamples => _samples.Where(s => s.IsSuccess);

    /// <summary>
    /// Gets only the failed samples
    /// </summary>
    public IEnumerable<Sample> FailedSamples => _samples.Where(s => s.IsFailure);

    /// <summary>
    /// Gets samples filtered by status
    /// </summary>
    public IEnumerable<Sample> SamplesWithStatus(SampleStatus status)
    {
        return _samples.Where(s => s.Status == status);
    }

    /// <summary>
    /// Gets the earliest sample timestamp in this collection
    /// </summary>
    public DateTime? EarliestTimestamp => _samples.IsEmpty ? null : _samples.Min(s => s.Timestamp);

    /// <summary>
    /// Gets the latest sample timestamp in this collection
    /// </summary>
    public DateTime? LatestTimestamp => _samples.IsEmpty ? null : _samples.Max(s => s.Timestamp);

    /// <summary>
    /// Gets the minimum latency in this collection
    /// </summary>
    public Latency? MinimumLatency
    {
        get
        {
            if (_samples.IsEmpty)
                return null;

            var minLatencyMs = _samples.Min(s => s.Duration.GetValueIn(LatencyUnit.Milliseconds));
            return new Latency(minLatencyMs, LatencyUnit.Milliseconds);
        }
    }

    /// <summary>
    /// Gets the maximum latency in this collection
    /// </summary>
    public Latency? MaximumLatency
    {
        get
        {
            if (_samples.IsEmpty)
                return null;

            var maxLatencyMs = _samples.Max(s => s.Duration.GetValueIn(LatencyUnit.Milliseconds));
            return new Latency(maxLatencyMs, LatencyUnit.Milliseconds);
        }
    }

    /// <summary>
    /// Gets an enumerator for iterating through samples
    /// </summary>
    public IEnumerator<Sample> GetEnumerator()
    {
        return _samples.GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SampleCollection other)
            return false;

        if (Count != other.Count)
            return false;

        return _samples.SequenceEqual(other._samples);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var sample in _samples)
        {
            hash.Add(sample);
        }
        return hash.ToHashCode();
    }

    public override string ToString() => $"SampleCollection[Count={Count}]";
}
