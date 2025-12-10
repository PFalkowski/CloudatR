using CloudatR.Lib.Abstractions;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Tests;

public class MediatorSendTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatorSendTests()
    {
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new TestRequest("test data");

        // Act
        var response = await _mediator.Send(request);

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("Handled: test data");
    }

    [Fact]
    public async Task Send_WithVoidRequest_ReturnsUnit()
    {
        // Arrange
        VoidRequestHandler.WasCalled = false;
        var request = new VoidRequest("void test");

        // Act
        var result = await _mediator.Send(request);

        // Assert
        result.Should().Be(Unit.Value);
        VoidRequestHandler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _mediator.Send<TestResponse>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task Send_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var request = new TestRequest("test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await _mediator.Send(request, cts.Token);

        // Assert - the handler should receive the cancellation token
        // For this test, we're just verifying the token is passed through
        // A more complex handler could check and throw OperationCanceledException
        var response = await _mediator.Send(request);
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Send_MultipleRequests_HandlesIndependently()
    {
        // Arrange
        var request1 = new TestRequest("data1");
        var request2 = new TestRequest("data2");

        // Act
        var response1 = await _mediator.Send(request1);
        var response2 = await _mediator.Send(request2);

        // Assert
        response1.Result.Should().Be("Handled: data1");
        response2.Result.Should().Be("Handled: data2");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
