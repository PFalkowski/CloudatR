using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using CloudatR.Lib.Abstractions;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Internal;

namespace CloudatR.Lib.CloudEvents;

/// <summary>
/// Factory for creating CloudEvent contexts with metadata caching.
/// </summary>
internal sealed class CloudEventContextFactory : ICloudEventContextFactory
{
    private readonly CloudatRConfiguration _configuration;
    private readonly ConcurrentDictionary<Type, CloudEventMetadata> _metadataCache = new();

    public CloudEventContextFactory(CloudatRConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public ICloudEventContext CreateContext<T>(T data) where T : notnull
    {
        // Use the runtime type, not the compile-time type
        var runtimeType = data.GetType();
        var metadata = GetOrCreateMetadata(runtimeType);

        return new CloudEventContext
        {
            EventId = Guid.NewGuid().ToString(),
            Source = metadata.Source,
            Type = metadata.Type,
            Time = DateTime.UtcNow,
            Subject = metadata.Subject,
            Extensions = CreateExtensions()
        };
    }

    private CloudEventMetadata GetOrCreateMetadata(Type type)
    {
        return _metadataCache.GetOrAdd(type, t =>
        {
            var attribute = t.GetCustomAttribute<CloudEventAttribute>();

            return new CloudEventMetadata
            {
                Type = attribute?.Type ?? GenerateDefaultType(t),
                Source = attribute?.Source ?? _configuration.DefaultSource,
                Subject = attribute?.Subject
            };
        });
    }

    private string GenerateDefaultType(Type type)
    {
        if (_configuration.TypeNameConvention != null)
        {
            return _configuration.TypeNameConvention(type);
        }

        // Default convention: com.{assembly}.{category}.{typename}
        var assembly = type.Assembly.GetName().Name?.ToLowerInvariant().Replace(".", "-") ?? "app";
        var category = GetCategory(type);
        var name = type.Name.ToLowerInvariant();

        return $"com.{assembly}.{category}.{name}";
    }

    private static string GetCategory(Type type)
    {
        // Check if it's a notification
        if (typeof(INotification).IsAssignableFrom(type))
        {
            return "events";
        }

        // Check if it's a request
        var requestInterfaces = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            .ToList();

        if (requestInterfaces.Any())
        {
            // Use naming convention to distinguish commands from queries
            if (type.Name.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
            {
                return "commands";
            }

            if (type.Name.EndsWith("Query", StringComparison.OrdinalIgnoreCase))
            {
                return "queries";
            }

            // Default to commands for requests
            return "commands";
        }

        return "messages";
    }

    private static CloudEventExtensions CreateExtensions()
    {
        // Populate from distributed tracing context if available
        var activity = Activity.Current;

        return new CloudEventExtensions
        {
            CorrelationId = activity?.RootId ?? activity?.TraceId.ToString(),
            CausationId = activity?.ParentId ?? activity?.ParentSpanId.ToString(),
            // UserId and TenantId would typically come from HttpContext, ClaimsPrincipal, etc.
            // These can be set by a custom pipeline behavior
        };
    }
}
