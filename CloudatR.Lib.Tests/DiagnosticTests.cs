using System.Reflection;
using CloudatR.Lib.Abstractions;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CloudatR.Lib.Tests;

[Collection("CloudatR Tests")]
public class DiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public DiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DebugNotificationHandlerRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        _output.WriteLine($"TestNotification assembly: {typeof(TestNotification).Assembly.FullName}");
        _output.WriteLine($"TestNotificationHandler1 assembly: {typeof(TestNotificationHandler1).Assembly.FullName}");

        // Check if handlers implement the interface
        var handler1Type = typeof(TestNotificationHandler1);
        var interfaces = handler1Type.GetInterfaces();
        _output.WriteLine($"TestNotificationHandler1 interfaces: {string.Join(", ", interfaces.Select(i => i.Name))}");

        var notificationHandlerInterface = interfaces.FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));
        _output.WriteLine($"Implements INotificationHandler<>: {notificationHandlerInterface != null}");

        if (notificationHandlerInterface != null)
        {
            var notificationType = notificationHandlerInterface.GetGenericArguments()[0];
            _output.WriteLine($"Notification type: {notificationType.Name}");
        }

        // Register with CloudatR
        services.AddCloudatR(typeof(TestNotification).Assembly);

        var provider = services.BuildServiceProvider();

        // Try to get handlers
        var handlers = provider.GetServices<INotificationHandler<TestNotification>>().ToList();
        _output.WriteLine($"Registered handlers count: {handlers.Count}");
        for (int i = 0; i < handlers.Count; i++)
        {
            var handler = handlers[i];
            _output.WriteLine($"Handler[{i}] type: {handler.GetType().Name}");

            // Check the Handle method
            var handleMethod = handler.GetType().GetMethod("Handle");
            if (handleMethod != null)
            {
                var parameters = handleMethod.GetParameters();
                _output.WriteLine($"  Handle method params: {string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
            }
        }

        // Check static lists before
        _output.WriteLine($"Before clear - Handler1 messages: {TestNotificationHandler1.ReceivedMessages.Count}");
        _output.WriteLine($"Before clear - Handler2 messages: {TestNotificationHandler2.ReceivedMessages.Count}");

        // Clear and verify
        TestNotificationHandler1.ReceivedMessages.Clear();
        TestNotificationHandler2.ReceivedMessages.Clear();

        _output.WriteLine($"After clear - Handler1 messages: {TestNotificationHandler1.ReceivedMessages.Count}");
        _output.WriteLine($"After clear - Handler2 messages: {TestNotificationHandler2.ReceivedMessages.Count}");

        // Check the cache via reflection
        var mediator = provider.GetRequiredService<IMediator>();
        var mediatorType = mediator.GetType();
        var cacheField = mediatorType.GetField("_handlerCache", BindingFlags.NonPublic | BindingFlags.Instance);
        if (cacheField != null)
        {
            var cache = cacheField.GetValue(mediator);
            var cacheType = cache!.GetType();
            var getNotificationHandlersMethod = cacheType.GetMethod("GetNotificationHandlers");
            if (getNotificationHandlersMethod != null)
            {
                var wrappers = getNotificationHandlersMethod.Invoke(cache, new object[] { typeof(TestNotification) });
                var wrappersArray = (System.Collections.IEnumerable)wrappers!;
                var count = wrappersArray.Cast<object>().Count();
                _output.WriteLine($"Wrappers in cache for TestNotification: {count}");
            }
        }

        // Try to publish
        var notification = new TestNotification("test");

        mediator.Publish(notification).Wait();

        _output.WriteLine($"After publish - Handler1 messages: {TestNotificationHandler1.ReceivedMessages.Count}");
        _output.WriteLine($"After publish - Handler2 messages: {TestNotificationHandler2.ReceivedMessages.Count}");

        foreach (var msg in TestNotificationHandler1.ReceivedMessages)
        {
            _output.WriteLine($"Handler1 msg: {msg}");
        }
        foreach (var msg in TestNotificationHandler2.ReceivedMessages)
        {
            _output.WriteLine($"Handler2 msg: {msg}");
        }

        // Verify the handlers themselves
        var handler1Instance = new TestNotificationHandler1();
        handler1Instance.Handle(new TestNotification("direct test"), CancellationToken.None).Wait();
        _output.WriteLine($"After direct call - Handler1 messages: {TestNotificationHandler1.ReceivedMessages.Count}");

        provider.Dispose();
    }
}
