namespace CloudatR.Lib.Abstractions;

/// <summary>
/// Defines the mediator interface for sending requests and publishing notifications.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Send a request to a single handler and get a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a notification to all registered handlers using the default strategy (SyncContinueOnException).
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Publish a notification to all registered handlers using the specified strategy.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="strategy">The publish strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
