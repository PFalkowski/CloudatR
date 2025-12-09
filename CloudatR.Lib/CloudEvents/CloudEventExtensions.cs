namespace CloudatR.Lib.CloudEvents;

/// <summary>
/// Default implementation of CloudEvents extension attributes.
/// </summary>
public sealed record CloudEventExtensions : ICloudEventExtensions
{
    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    public string? CausationId { get; init; }

    /// <inheritdoc />
    public string? UserId { get; init; }

    /// <inheritdoc />
    public string? TenantId { get; init; }

    /// <inheritdoc />
    public Dictionary<string, object>? CustomAttributes { get; init; }
}
