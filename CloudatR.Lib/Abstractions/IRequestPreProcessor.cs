namespace CloudatR.Lib.Abstractions;

/// <summary>
/// Defines a pre-processor for a request.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
public interface IRequestPreProcessor<in TRequest>
{
    /// <summary>
    /// Process method called before the request is handled.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Process(TRequest request, CancellationToken cancellationToken);
}
