using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;

namespace CloudatR.Lib.Internal;

/// <summary>
/// Wrapper for notification handlers that provides typed invocation.
/// </summary>
internal sealed class NotificationHandlerWrapper
{
    private readonly Func<IServiceProvider, object> _handlerFactory;
    private readonly Type _notificationType;

    public NotificationHandlerWrapper(
        Func<IServiceProvider, object> handlerFactory,
        Type notificationType)
    {
        _handlerFactory = handlerFactory;
        _notificationType = notificationType;
    }

    /// <summary>
    /// Handles a notification by invoking the handler.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="context">The CloudEvent context.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(
        object notification,
        ICloudEventContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = _handlerFactory(serviceProvider);

        // Get the Handle method
        var handleMethod = handler.GetType()
            .GetMethod(nameof(INotificationHandler<INotification>.Handle));

        if (handleMethod == null)
        {
            throw new InvalidOperationException(
                $"Handler {handler.GetType().Name} does not have a Handle method.");
        }

        // Invoke the handler
        var task = (Task?)handleMethod.Invoke(handler, new[] { notification, cancellationToken });
        if (task != null)
        {
            await task;
        }
    }
}
