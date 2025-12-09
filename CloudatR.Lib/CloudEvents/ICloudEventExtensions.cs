namespace CloudatR.Lib.CloudEvents;

/// <summary>
/// Defines standard CloudEvents extension attributes for correlation, causation, and context.
/// </summary>
public interface ICloudEventExtensions
{
    /// <summary>
    /// Correlation ID for linking related events across a distributed system.
    /// </summary>
    string? CorrelationId { get; init; }

    /// <summary>
    /// Causation ID that identifies the command/event that caused this event.
    /// </summary>
    string? CausationId { get; init; }

    /// <summary>
    /// User ID of the user who initiated the request.
    /// </summary>
    string? UserId { get; init; }

    /// <summary>
    /// Tenant ID for multi-tenant scenarios.
    /// </summary>
    string? TenantId { get; init; }

    /// <summary>
    /// Custom extension attributes beyond the standard set.
    /// </summary>
    Dictionary<string, object>? CustomAttributes { get; init; }
}
