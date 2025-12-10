using CloudatR.Lib.Abstractions;

namespace CloudatR.Lib.Tests.TestHelpers;

public class TestRequestPreProcessor : IRequestPreProcessor<TestRequest>
{
    public static List<string> ProcessedRequests { get; } = new();

    public Task Process(TestRequest request, CancellationToken cancellationToken)
    {
        ProcessedRequests.Add($"PreProcess: {request.Data}");
        return Task.CompletedTask;
    }
}

public class TestRequestPostProcessor : IRequestPostProcessor<TestRequest, TestResponse>
{
    public static List<string> ProcessedResponses { get; } = new();

    public Task Process(TestRequest request, TestResponse response, CancellationToken cancellationToken)
    {
        ProcessedResponses.Add($"PostProcess: {response.Result}");
        return Task.CompletedTask;
    }
}
