using System.Reflection;
using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;
using CloudatR.Lib.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CloudatR.Lib.DependencyInjection;

/// <summary>
/// Extension methods for registering CloudatR services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CloudatR services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCloudatR(
        this IServiceCollection services,
        Action<CloudatRConfiguration>? configure = null)
    {
        var configuration = new CloudatRConfiguration();
        configure?.Invoke(configuration);

        // Register configuration as singleton
        services.TryAddSingleton(configuration);

        // Register core services
        services.TryAddSingleton<IMediatoRHandlerCache, MediatoRHandlerCache>();
        services.TryAddSingleton<ICloudEventContextFactory, CloudEventContextFactory>();

        // Register CloudEventContext as both the concrete type and interface
        // This allows us to mutate the scoped instance during request handling
        services.TryAddScoped<CloudEventContext>();
        services.TryAddScoped<ICloudEventContext>(sp => sp.GetRequiredService<CloudEventContext>());

        services.TryAddTransient<IMediator, Mediator>();

        // Register handlers from assemblies
        var handlerCache = new MediatoRHandlerCache();
        foreach (var assembly in configuration.Assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly, configuration, handlerCache);
        }

        // Register the populated cache as singleton
        services.AddSingleton<IMediatoRHandlerCache>(handlerCache);

        // Note: Global behaviors are not registered here.
        // They should be registered manually by scanning assemblies for types implementing IPipelineBehavior
        // or we register them when we discover request handlers

        return services;
    }

    /// <summary>
    /// Adds CloudatR services and registers handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCloudatR(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddCloudatR(config =>
        {
            foreach (var assembly in assemblies)
            {
                config.RegisterServicesFromAssembly(assembly);
            }
        });
    }

    private static void RegisterHandlersFromAssembly(
        IServiceCollection services,
        Assembly assembly,
        CloudatRConfiguration configuration,
        MediatoRHandlerCache handlerCache)
    {
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToList();

        // Register request handlers
        RegisterRequestHandlers(services, types, configuration, handlerCache);

        // Register notification handlers
        RegisterNotificationHandlers(services, types, configuration, handlerCache);

        // Register pipeline behaviors
        RegisterPipelineBehaviors(services, types, configuration);

        // Register pre/post processors
        RegisterPreProcessors(services, types, configuration);
        RegisterPostProcessors(services, types, configuration);
    }

    private static void RegisterRequestHandlers(
        IServiceCollection services,
        List<Type> types,
        CloudatRConfiguration configuration,
        MediatoRHandlerCache handlerCache)
    {
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                var genericArgs = @interface.GetGenericArguments();
                var requestType = genericArgs[0];
                var responseType = genericArgs[1];

                // Register the handler
                services.Add(new ServiceDescriptor(@interface, type, configuration.HandlerLifetime));

                // Cache the handler wrapper
                handlerCache.CacheRequestHandler(requestType, responseType);
            }
        }
    }

    private static void RegisterNotificationHandlers(
        IServiceCollection services,
        List<Type> types,
        CloudatRConfiguration configuration,
        MediatoRHandlerCache handlerCache)
    {
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                var notificationType = @interface.GetGenericArguments()[0];

                // Register the handler
                services.Add(new ServiceDescriptor(@interface, type, configuration.HandlerLifetime));

                // Cache the handler wrapper
                handlerCache.CacheNotificationHandler(notificationType, @interface);
            }
        }
    }

    private static void RegisterPipelineBehaviors(
        IServiceCollection services,
        List<Type> types,
        CloudatRConfiguration configuration)
    {
        foreach (var type in types)
        {
            // Skip open generic types - they can't be instantiated directly
            if (type.IsGenericTypeDefinition)
            {
                continue;
            }

            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                // Register as transient (behaviors are typically lightweight)
                services.Add(new ServiceDescriptor(@interface, type, ServiceLifetime.Transient));
            }
        }
    }

    private static void RegisterPreProcessors(
        IServiceCollection services,
        List<Type> types,
        CloudatRConfiguration configuration)
    {
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                services.Add(new ServiceDescriptor(@interface, type, ServiceLifetime.Transient));
            }
        }
    }

    private static void RegisterPostProcessors(
        IServiceCollection services,
        List<Type> types,
        CloudatRConfiguration configuration)
    {
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPostProcessor<,>))
                .ToList();

            foreach (var @interface in interfaces)
            {
                services.Add(new ServiceDescriptor(@interface, type, ServiceLifetime.Transient));
            }
        }
    }
}
