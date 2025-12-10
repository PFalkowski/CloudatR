using CloudatR.Lib.Abstractions;

namespace CloudatR.Lib.Tests.TestHelpers;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> Logs { get; } = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Logs.Add($"Before: {typeof(TRequest).Name}");
        var response = await next();
        Logs.Add($"After: {typeof(TRequest).Name}");
        return response;
    }
}

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static bool WasCalled { get; set; }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return await next();
    }
}
