namespace CloudatR.Lib.Abstractions;

/// <summary>
/// Defines a post-processor for a request and response.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse>
{
    /// <summary>
    /// Process method called after the request is handled.
    /// </summary>
    /// <param name="request">The request that was handled.</param>
    /// <param name="response">The response from the handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}
