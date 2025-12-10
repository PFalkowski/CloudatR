using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Tests;

[Collection("CloudatR Tests")]
public class IntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public IntegrationTests()
    {
        // Reset static state before creating service provider
        LoggingBehavior<TestRequest, TestResponse>.Logs.Clear();
        TestRequestPreProcessor.ProcessedRequests.Clear();
        TestRequestPostProcessor.ProcessedResponses.Clear();
        TestNotificationHandler1.ReceivedMessages.Clear();
        TestNotificationHandler2.ReceivedMessages.Clear();

        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(TestRequest).Assembly);
            config.DefaultSource = "integration-tests";
        });

        services.AddTransient<IPipelineBehavior<TestRequest, TestResponse>, LoggingBehavior<TestRequest, TestResponse>>();
        services.AddTransient<IRequestPreProcessor<TestRequest>, TestRequestPreProcessor>();
        services.AddTransient<IRequestPostProcessor<TestRequest, TestResponse>, TestRequestPostProcessor>();

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task FullPipeline_WithRequestHandlerBehaviorsAndProcessors_ExecutesCorrectly()
    {
        // Arrange
        var request = new TestRequest("integration test");

        // Act
        var response = await _mediator.Send(request);

        // Assert
        response.Result.Should().Be("Handled: integration test");

        // Verify all pipeline components were executed
        TestRequestPreProcessor.ProcessedRequests.Should().Contain("PreProcess: integration test");
        LoggingBehavior<TestRequest, TestResponse>.Logs.Should().HaveCount(2);
        TestRequestPostProcessor.ProcessedResponses.Should().Contain("PostProcess: Handled: integration test");
    }

    [Fact]
    public async Task CloudEventContext_IsPopulatedForRequest()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cloudEventContext = scope.ServiceProvider.GetRequiredService<ICloudEventContext>();

        var request = new TestRequest("test");

        // Act
        await mediator.Send(request);

        // Assert
        cloudEventContext.EventId.Should().NotBeNullOrEmpty();
        cloudEventContext.Source.Should().Be("integration-tests");
        cloudEventContext.Type.Should().Contain("testrequest");
        cloudEventContext.Time.Should().NotBeNull();
    }

    [Fact]
    public async Task CloudEventContext_IsPopulatedForNotification()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cloudEventContext = scope.ServiceProvider.GetRequiredService<ICloudEventContext>();

        var notification = new TestNotification("test");

        // Act
        await mediator.Publish(notification);

        // Assert
        cloudEventContext.EventId.Should().NotBeNullOrEmpty();
        cloudEventContext.Source.Should().Be("integration-tests");
        cloudEventContext.Type.Should().Contain("testnotification");
        cloudEventContext.Time.Should().NotBeNull();
    }

    [Fact]
    public async Task MultipleScopes_HaveIndependentCloudEventContexts()
    {
        // Arrange
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        var context1 = scope1.ServiceProvider.GetRequiredService<ICloudEventContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<ICloudEventContext>();

        var request = new TestRequest("test");

        // Act
        await mediator1.Send(request);
        await mediator2.Send(request);

        // Assert
        context1.EventId.Should().NotBe(context2.EventId);
    }

    [Fact]
    public async Task ConcurrentRequests_AreHandledCorrectly()
    {
        // Arrange
        var requests = Enumerable.Range(1, 10)
            .Select(i => new TestRequest($"request-{i}"))
            .ToList();

        // Act
        var tasks = requests.Select(r => _mediator.Send(r));
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Select(r => r.Result).Should().AllSatisfy(result =>
            result.Should().StartWith("Handled: request-"));
    }

    [Fact]
    public async Task ConcurrentNotifications_AreHandledCorrectly()
    {
        // Arrange
        TestNotificationHandler1.ReceivedMessages.Clear();
        TestNotificationHandler2.ReceivedMessages.Clear();

        var notifications = Enumerable.Range(1, 10)
            .Select(i => new TestNotification($"notification-{i}"))
            .ToList();

        // Act
        var tasks = notifications.Select(n => _mediator.Publish(n));
        await Task.WhenAll(tasks);

        // Assert
        TestNotificationHandler1.ReceivedMessages.Should().HaveCount(10);
        TestNotificationHandler2.ReceivedMessages.Should().HaveCount(10);
    }

    [Fact]
    public async Task MixedRequestsAndNotifications_WorkTogether()
    {
        // Arrange
        TestNotificationHandler1.ReceivedMessages.Clear();
        TestNotificationHandler2.ReceivedMessages.Clear();

        var request = new TestRequest("mixed test");
        var notification = new TestNotification("mixed notification");

        // Act
        var response = await _mediator.Send(request);
        await _mediator.Publish(notification);

        // Assert
        response.Result.Should().Be("Handled: mixed test");
        TestNotificationHandler1.ReceivedMessages.Should().Contain("Handler1: mixed notification");
        TestNotificationHandler2.ReceivedMessages.Should().Contain("Handler2: mixed notification");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
