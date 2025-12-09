namespace CloudatR.Lib.CloudEvents;

/// <summary>
/// Internal implementation of CloudEvent context that gets populated by the mediator.
/// </summary>
internal sealed class CloudEventContext : ICloudEventContext
{
    /// <inheritdoc />
    public string EventId { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Source { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Type { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime? Time { get; set; }

    /// <inheritdoc />
    public string? Subject { get; set; }

    /// <inheritdoc />
    public ICloudEventExtensions Extensions { get; set; } = new CloudEventExtensions();
}
