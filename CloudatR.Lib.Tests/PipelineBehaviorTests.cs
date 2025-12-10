using CloudatR.Lib.Abstractions;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Tests;

[Collection("CloudatR Tests")]
public class PipelineBehaviorTests : IDisposable
{
    private ServiceProvider? _serviceProvider;

    [Fact]
    public async Task Send_WithBehavior_ExecutesBehaviorAroundHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });
        services.AddTransient<IPipelineBehavior<TestRequest, TestResponse>, LoggingBehavior<TestRequest, TestResponse>>();

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        LoggingBehavior<TestRequest, TestResponse>.Logs.Clear();

        var request = new TestRequest("test");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: test");
        LoggingBehavior<TestRequest, TestResponse>.Logs.Should().HaveCount(2);
        LoggingBehavior<TestRequest, TestResponse>.Logs[0].Should().Be("Before: TestRequest");
        LoggingBehavior<TestRequest, TestResponse>.Logs[1].Should().Be("After: TestRequest");
    }

    [Fact]
    public async Task Send_WithMultipleBehaviors_ExecutesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });
        services.AddTransient<IPipelineBehavior<TestRequest, TestResponse>, LoggingBehavior<TestRequest, TestResponse>>();
        services.AddTransient<IPipelineBehavior<TestRequest, TestResponse>, ValidationBehavior<TestRequest, TestResponse>>();

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        LoggingBehavior<TestRequest, TestResponse>.Logs.Clear();
        ValidationBehavior<TestRequest, TestResponse>.WasCalled = false;

        var request = new TestRequest("test");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: test");
        ValidationBehavior<TestRequest, TestResponse>.WasCalled.Should().BeTrue();
        LoggingBehavior<TestRequest, TestResponse>.Logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task Send_WithoutBehaviors_ExecutesHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        var request = new TestRequest("test");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: test");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

public class PrePostProcessorTests : IDisposable
{
    private ServiceProvider? _serviceProvider;

    [Fact]
    public async Task Send_WithPreProcessor_ExecutesBeforeHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });
        services.AddTransient<IRequestPreProcessor<TestRequest>, TestRequestPreProcessor>();

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        TestRequestPreProcessor.ProcessedRequests.Clear();

        var request = new TestRequest("test data");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: test data");
        TestRequestPreProcessor.ProcessedRequests.Should().Contain("PreProcess: test data");
    }

    [Fact]
    public async Task Send_WithPostProcessor_ExecutesAfterHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });
        services.AddTransient<IRequestPostProcessor<TestRequest, TestResponse>, TestRequestPostProcessor>();

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        TestRequestPostProcessor.ProcessedResponses.Clear();

        var request = new TestRequest("test data");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: test data");
        TestRequestPostProcessor.ProcessedResponses.Should().Contain("PostProcess: Handled: test data");
    }

    [Fact]
    public async Task Send_WithPreAndPostProcessors_ExecutesInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });
        services.AddTransient<IRequestPreProcessor<TestRequest>, TestRequestPreProcessor>();
        services.AddTransient<IRequestPostProcessor<TestRequest, TestResponse>, TestRequestPostProcessor>();

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        TestRequestPreProcessor.ProcessedRequests.Clear();
        TestRequestPostProcessor.ProcessedResponses.Clear();

        var request = new TestRequest("test data");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: test data");
        TestRequestPreProcessor.ProcessedRequests.Should().Contain("PreProcess: test data");
        TestRequestPostProcessor.ProcessedResponses.Should().Contain("PostProcess: Handled: test data");
    }

    [Fact]
    public async Task Send_WithBehaviorsAndProcessors_ExecutesInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });
        services.AddTransient<IRequestPreProcessor<TestRequest>, TestRequestPreProcessor>();
        services.AddTransient<IPipelineBehavior<TestRequest, TestResponse>, LoggingBehavior<TestRequest, TestResponse>>();
        services.AddTransient<IRequestPostProcessor<TestRequest, TestResponse>, TestRequestPostProcessor>();

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        TestRequestPreProcessor.ProcessedRequests.Clear();
        LoggingBehavior<TestRequest, TestResponse>.Logs.Clear();
        TestRequestPostProcessor.ProcessedResponses.Clear();

        var request = new TestRequest("test data");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: test data");

        // Verify order: PreProcessor -> Behavior(Before) -> Handler -> Behavior(After) -> PostProcessor
        TestRequestPreProcessor.ProcessedRequests.Should().Contain("PreProcess: test data");
        LoggingBehavior<TestRequest, TestResponse>.Logs.Should().HaveCount(2);
        LoggingBehavior<TestRequest, TestResponse>.Logs[0].Should().Be("Before: TestRequest");
        LoggingBehavior<TestRequest, TestResponse>.Logs[1].Should().Be("After: TestRequest");
        TestRequestPostProcessor.ProcessedResponses.Should().Contain("PostProcess: Handled: test data");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
