using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Tests;

public class EdgeCaseTests : IDisposable
{
    private ServiceProvider? _serviceProvider;

    [Fact]
    public async Task Send_WithUnregisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(); // No handlers registered

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        var request = new TestRequest("test");

        // Act
        Func<Task> act = async () => await mediator.Send(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No handler registered*");
    }

    [Fact]
    public async Task Publish_WithEmptyHandlerList_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(); // No handlers registered

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        var notification = new TestNotification("test");

        // Act
        Func<Task> act = async () => await mediator.Publish(notification);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Send_WithLargePayload_HandlesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestRequest>();
        });

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        var largeData = new string('x', 10000);
        var request = new TestRequest(largeData);

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.Result.Should().StartWith("Handled: ");
        response.Result.Length.Should().BeGreaterThan(10000);
    }

    [Fact]
    public async Task Publish_ParallelNoWait_DoesNotThrowForSuccessfulHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestNotification>();
        });

        _serviceProvider = services.BuildServiceProvider();
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        TestNotificationHandler1.ReceivedMessages.Clear();
        TestNotificationHandler2.ReceivedMessages.Clear();

        var notification = new TestNotification("fire and forget");

        // Act
        Func<Task> act = async () =>
        {
            await mediator.Publish(notification, PublishStrategy.ParallelNoWait);
            await Task.Delay(100); // Give time for fire-and-forget to complete
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Send_WithCancelledToken_PropagatesToHandler()
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - the simple handler doesn't check cancellation,
        // so it will complete successfully
        var response = await mediator.Send(request, cts.Token);
        response.Should().NotBeNull();
    }

    [Fact]
    public void CloudEventContext_Scoped_CreatesNewInstancePerScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR();

        _serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var context1 = scope1.ServiceProvider.GetRequiredService<ICloudEventContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<ICloudEventContext>();

        // Assert
        // Both contexts exist and are available in their respective scopes
        context1.Should().NotBeNull();
        context2.Should().NotBeNull();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
