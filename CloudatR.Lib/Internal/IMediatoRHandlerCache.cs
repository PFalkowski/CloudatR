namespace CloudatR.Lib.Internal;

/// <summary>
/// Cache for handler wrappers to avoid reflection in the hot path.
/// </summary>
internal interface IMediatoRHandlerCache
{
    /// <summary>
    /// Gets a cached request handler wrapper for the specified request type.
    /// </summary>
    /// <param name="requestType">The type of request.</param>
    /// <param name="responseType">The type of response.</param>
    /// <returns>The handler wrapper.</returns>
    RequestHandlerWrapper GetRequestHandler(Type requestType, Type responseType);

    /// <summary>
    /// Gets all cached notification handler wrappers for the specified notification type.
    /// </summary>
    /// <param name="notificationType">The type of notification.</param>
    /// <returns>A list of handler wrappers.</returns>
    IReadOnlyList<NotificationHandlerWrapper> GetNotificationHandlers(Type notificationType);
}
