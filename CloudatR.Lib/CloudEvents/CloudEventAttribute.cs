namespace CloudatR.Lib.CloudEvents;

/// <summary>
/// Attribute for customizing CloudEvent metadata on request and notification types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CloudEventAttribute : Attribute
{
    /// <summary>
    /// Custom CloudEvent type. Overrides the default convention.
    /// Example: "com.example.order.created"
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Custom CloudEvent source. Overrides the default source.
    /// Example: "https://order-service.example.com" or "order-service"
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// CloudEvent subject identifying the subject of the event.
    /// Example: "/orders/123"
    /// </summary>
    public string? Subject { get; init; }
}
