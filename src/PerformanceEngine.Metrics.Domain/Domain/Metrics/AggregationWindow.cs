namespace PerformanceEngine.Metrics.Domain.Metrics;

/// <summary>
/// Abstract base class representing a window for aggregating samples.
/// Defines how samples should be grouped (full execution, sliding, fixed windows).
/// </summary>
public abstract class AggregationWindow
{
    /// <summary>
    /// Gets the name/type of this aggregation window
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets a description of this aggregation window
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Factory method to create a full execution window (all samples).
    /// </summary>
    public static FullExecutionWindow FullExecution()
    {
        return new FullExecutionWindow();
    }

    /// <summary>
    /// Factory method to create a fixed window (non-overlapping intervals).
    /// </summary>
    /// <param name="windowSize">The size of each window</param>
    /// <returns>A new FixedWindow instance</returns>
    /// <exception cref="ArgumentException">Thrown when windowSize is not positive</exception>
    public static FixedWindow Fixed(TimeSpan windowSize)
    {
        return new FixedWindow(windowSize);
    }

    /// <summary>
    /// Factory method to create a sliding window (overlapping intervals).
    /// </summary>
    /// <param name="windowSize">The size of each window</param>
    /// <param name="slideInterval">The interval at which to advance the window</param>
    /// <returns>A new SlidingWindow instance</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public static SlidingWindow Sliding(TimeSpan windowSize, TimeSpan slideInterval)
    {
        return new SlidingWindow(windowSize, slideInterval);
    }
}

/// <summary>
/// Aggregation window that encompasses the entire execution period.
/// </summary>
public sealed class FullExecutionWindow : AggregationWindow
{
    public override string Name => "FullExecution";
    public override string Description => "All samples from start to end of execution";
}

/// <summary>
/// Fixed-size non-overlapping windows for aggregation.
/// </summary>
public sealed class FixedWindow : AggregationWindow
{
    /// <summary>
    /// Gets the size of each fixed window
    /// </summary>
    public TimeSpan WindowSize { get; }

    public FixedWindow(TimeSpan windowSize)
    {
        if (windowSize <= TimeSpan.Zero)
        {
            throw new ArgumentException("Window size must be positive", nameof(windowSize));
        }

        WindowSize = windowSize;
    }

    public override string Name => "FixedWindow";
    public override string Description => $"Fixed windows of {WindowSize.TotalMilliseconds}ms";
}

/// <summary>
/// Sliding window where windows overlap.
/// </summary>
public sealed class SlidingWindow : AggregationWindow
{
    /// <summary>
    /// Gets the size of each sliding window
    /// </summary>
    public TimeSpan WindowSize { get; }

    /// <summary>
    /// Gets the interval at which windows advance/slide
    /// </summary>
    public TimeSpan SlideInterval { get; }

    public SlidingWindow(TimeSpan windowSize, TimeSpan slideInterval)
    {
        if (windowSize <= TimeSpan.Zero)
        {
            throw new ArgumentException("Window size must be positive", nameof(windowSize));
        }

        if (slideInterval <= TimeSpan.Zero)
        {
            throw new ArgumentException("Slide interval must be positive", nameof(slideInterval));
        }

        if (slideInterval > windowSize)
        {
            throw new ArgumentException(
                "Slide interval cannot be larger than window size",
                nameof(slideInterval));
        }

        WindowSize = windowSize;
        SlideInterval = slideInterval;
    }

    public override string Name => "SlidingWindow";
    public override string Description => $"Sliding windows of {WindowSize.TotalMilliseconds}ms, advancing every {SlideInterval.TotalMilliseconds}ms";
}
