using CloudatR.Lib.Abstractions;

namespace CloudatR.Lib.Tests.TestHelpers;

public record TestNotification(string Message) : INotification;

public class TestNotificationHandler1 : INotificationHandler<TestNotification>
{
    public static List<string> ReceivedMessages { get; } = new();

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        ReceivedMessages.Add($"Handler1: {notification.Message}");
        return Task.CompletedTask;
    }
}

public class TestNotificationHandler2 : INotificationHandler<TestNotification>
{
    public static List<string> ReceivedMessages { get; } = new();

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        ReceivedMessages.Add($"Handler2: {notification.Message}");
        return Task.CompletedTask;
    }
}
