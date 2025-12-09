namespace CloudatR.Lib.Internal;

/// <summary>
/// Cached metadata for CloudEvent generation.
/// </summary>
internal sealed class CloudEventMetadata
{
    /// <summary>
    /// The CloudEvent type for this message.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The CloudEvent source for this message.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The CloudEvent subject for this message.
    /// </summary>
    public string? Subject { get; init; }
}
