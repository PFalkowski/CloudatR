using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;
using CloudatR.Lib.Internal;

namespace CloudatR.Lib;

/// <summary>
/// Default implementation of the mediator pattern with CloudEvents support.
/// </summary>
internal sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediatoRHandlerCache _handlerCache;
    private readonly ICloudEventContextFactory _contextFactory;

    public Mediator(
        IServiceProvider serviceProvider,
        IMediatoRHandlerCache handlerCache,
        ICloudEventContextFactory contextFactory)
    {
        _serviceProvider = serviceProvider;
        _handlerCache = handlerCache;
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestType = request.GetType();

        // Create CloudEvent context for this request
        var cloudEventContext = _contextFactory.CreateContext(request);

        // Get the cached handler wrapper
        var handlerWrapper = _handlerCache.GetRequestHandler(requestType, typeof(TResponse));

        // Execute through pipeline
        return await handlerWrapper.Handle(request, cloudEventContext, _serviceProvider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        return Publish(notification, PublishStrategy.SyncContinueOnException, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Publish<TNotification>(
        TNotification notification,
        PublishStrategy strategy,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        var notificationType = notification.GetType();

        // Create CloudEvent context for this notification
        var cloudEventContext = _contextFactory.CreateContext(notification);

        // Get all notification handlers
        var handlerWrappers = _handlerCache.GetNotificationHandlers(notificationType);

        if (handlerWrappers.Count == 0)
        {
            // No handlers registered, nothing to do
            return;
        }

        // Execute based on strategy
        await PublishInternal(
            handlerWrappers,
            notification,
            cloudEventContext,
            strategy,
            _serviceProvider,
            cancellationToken);
    }

    private static async Task PublishInternal(
        IReadOnlyList<NotificationHandlerWrapper> handlers,
        object notification,
        ICloudEventContext context,
        PublishStrategy strategy,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        switch (strategy)
        {
            case PublishStrategy.SyncContinueOnException:
                await PublishSyncContinueOnException(handlers, notification, context, serviceProvider, cancellationToken);
                break;

            case PublishStrategy.SyncStopOnException:
                await PublishSyncStopOnException(handlers, notification, context, serviceProvider, cancellationToken);
                break;

            case PublishStrategy.Async:
                await PublishAsync(handlers, notification, context, serviceProvider, cancellationToken);
                break;

            case PublishStrategy.ParallelNoWait:
                PublishParallelNoWait(handlers, notification, context, serviceProvider, cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Invalid publish strategy.");
        }
    }

    private static async Task PublishSyncContinueOnException(
        IReadOnlyList<NotificationHandlerWrapper> handlers,
        object notification,
        ICloudEventContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(notification, context, serviceProvider, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException("One or more notification handlers threw an exception.", exceptions);
        }
    }

    private static async Task PublishSyncStopOnException(
        IReadOnlyList<NotificationHandlerWrapper> handlers,
        object notification,
        ICloudEventContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, context, serviceProvider, cancellationToken);
        }
    }

    private static async Task PublishAsync(
        IReadOnlyList<NotificationHandlerWrapper> handlers,
        object notification,
        ICloudEventContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var tasks = handlers.Select(handler =>
            handler.Handle(notification, context, serviceProvider, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private static void PublishParallelNoWait(
        IReadOnlyList<NotificationHandlerWrapper> handlers,
        object notification,
        ICloudEventContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await handler.Handle(notification, context, serviceProvider, cancellationToken);
                }
                catch
                {
                    // Fire and forget - swallow exceptions
                }
            }, cancellationToken);
        }
    }
}
