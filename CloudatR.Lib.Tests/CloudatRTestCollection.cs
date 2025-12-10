using Xunit;

namespace CloudatR.Lib.Tests;

/// <summary>
/// Defines a test collection to ensure tests that share static state run serially.
/// This prevents test pollution from parallel execution.
/// </summary>
[CollectionDefinition("CloudatR Tests")]
public class CloudatRTestCollection
{
    // This class is never instantiated
}
