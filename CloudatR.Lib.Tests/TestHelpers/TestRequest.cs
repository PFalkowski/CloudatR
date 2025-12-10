using CloudatR.Lib.Abstractions;

namespace CloudatR.Lib.Tests.TestHelpers;

public record TestRequest(string Data) : IRequest<TestResponse>;

public record TestResponse(string Result);

public class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
{
    public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse($"Handled: {request.Data}"));
    }
}

public record VoidRequest(string Data) : IRequest;

public class VoidRequestHandler : IRequestHandler<VoidRequest, Unit>
{
    public static bool WasCalled { get; set; }

    public Task<Unit> Handle(VoidRequest request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(Unit.Value);
    }
}
