namespace CloudatR.Lib;

/// <summary>
/// Defines the strategy for publishing notifications to multiple handlers.
/// </summary>
public enum PublishStrategy
{
    /// <summary>
    /// Run handlers sequentially, continuing even if an exception occurs.
    /// </summary>
    SyncContinueOnException = 0,

    /// <summary>
    /// Run handlers sequentially, stopping on the first exception.
    /// </summary>
    SyncStopOnException = 1,

    /// <summary>
    /// Run all handlers in parallel using Task.WhenAll.
    /// </summary>
    Async = 2,

    /// <summary>
    /// Fire and forget - start all handlers in parallel without waiting.
    /// </summary>
    ParallelNoWait = 3
}
