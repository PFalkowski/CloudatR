namespace CloudatR.Lib.CloudEvents;

/// <summary>
/// Factory for creating CloudEvent contexts from messages.
/// </summary>
internal interface ICloudEventContextFactory
{
    /// <summary>
    /// Creates a CloudEvent context for the given data.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="data">The message data.</param>
    /// <returns>A CloudEvent context.</returns>
    ICloudEventContext CreateContext<T>(T data) where T : notnull;
}
