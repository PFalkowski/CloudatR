namespace CloudatR.Lib.CloudEvents;

/// <summary>
/// Provides access to CloudEvent metadata during request/notification processing.
/// This service is registered as scoped and populated by the mediator.
/// </summary>
public interface ICloudEventContext
{
    /// <summary>
    /// Uniquely identifies the event within the scope of the producer.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// A URI-reference identifying the context in which the event happened.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Describes the type of event related to the originating occurrence.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Timestamp of when the occurrence happened.
    /// </summary>
    DateTime? Time { get; }

    /// <summary>
    /// Identifies the subject of the event in the context of the event producer.
    /// </summary>
    string? Subject { get; }

    /// <summary>
    /// CloudEvents extension attributes for correlation, causation, and custom metadata.
    /// </summary>
    ICloudEventExtensions Extensions { get; }
}
