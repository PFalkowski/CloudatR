using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;

namespace CloudatR.Lib.Internal;

/// <summary>
/// Abstract base class for request handler wrappers.
/// </summary>
internal abstract class RequestHandlerWrapper
{
    /// <summary>
    /// Handles a request through the pipeline and returns a response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to handle.</param>
    /// <param name="context">The CloudEvent context.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    public abstract Task<TResponse> Handle<TResponse>(
        IRequest<TResponse> request,
        ICloudEventContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
