namespace CloudatR.Lib.Abstractions;

/// <summary>
/// Marker interface to represent a request with a response.
/// </summary>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IRequest<out TResponse>
{
}

/// <summary>
/// Marker interface to represent a request with no response (void-like).
/// </summary>
public interface IRequest : IRequest<Unit>
{
}
