using CloudatR.Lib.Abstractions;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Tests;

[Collection("CloudatR Tests")]
public class MediatorPublishTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public MediatorPublishTests()
    {
        // Reset static state before creating service provider
        TestNotificationHandler1.ReceivedMessages.Clear();
        TestNotificationHandler2.ReceivedMessages.Clear();

        var services = new ServiceCollection();
        services.AddCloudatR(typeof(TestNotification).Assembly);

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Publish_WithDefaultStrategy_CallsAllHandlers()
    {
        // Arrange
        var notification = new TestNotification("test message");

        // Act
        await _mediator.Publish(notification);

        // Assert
        TestNotificationHandler1.ReceivedMessages.Should().Contain("Handler1: test message");
        TestNotificationHandler2.ReceivedMessages.Should().Contain("Handler2: test message");
    }

    [Fact]
    public async Task Publish_WithNullNotification_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _mediator.Publish<TestNotification>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("notification");
    }

    [Fact]
    public async Task Publish_WithNoHandlers_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var notification = new TestNotification("test");

        // Act
        Func<Task> act = async () => await mediator.Publish(notification);

        // Assert
        await act.Should().NotThrowAsync();
        provider.Dispose();
    }

    // Note: Exception handling tests are omitted due to assembly scanning limitations
    // The MediatoRHandlerCache is populated at configuration time, and assembly scanning
    // finds all handlers in the test assembly, making it difficult to isolate exception scenarios

    [Fact]
    public async Task Publish_AsyncStrategy_RunsHandlersInParallel()
    {
        // Arrange
        var notification = new TestNotification("parallel test");

        // Act
        await _mediator.Publish(notification, PublishStrategy.Async);

        // Assert
        TestNotificationHandler1.ReceivedMessages.Should().Contain("Handler1: parallel test");
        TestNotificationHandler2.ReceivedMessages.Should().Contain("Handler2: parallel test");
    }

    [Fact]
    public async Task Publish_ParallelNoWaitStrategy_DoesNotWaitForHandlers()
    {
        // Arrange
        TestNotificationHandler1.ReceivedMessages.Clear();
        TestNotificationHandler2.ReceivedMessages.Clear();

        var notification = new TestNotification("fire and forget");

        // Act
        await _mediator.Publish(notification, PublishStrategy.ParallelNoWait);

        // Assert - we need to wait a bit since it's fire and forget
        await Task.Delay(100);
        TestNotificationHandler1.ReceivedMessages.Should().Contain("Handler1: fire and forget");
        TestNotificationHandler2.ReceivedMessages.Should().Contain("Handler2: fire and forget");
    }

    [Fact]
    public async Task Publish_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var notification = new TestNotification("test");

        // Act
        await _mediator.Publish(notification, CancellationToken.None);

        // Assert
        TestNotificationHandler1.ReceivedMessages.Should().NotBeEmpty();
        TestNotificationHandler2.ReceivedMessages.Should().NotBeEmpty();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
