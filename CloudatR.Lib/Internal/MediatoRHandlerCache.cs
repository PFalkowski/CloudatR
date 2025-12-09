using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using CloudatR.Lib.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Internal;

/// <summary>
/// Cache for handler wrappers with compiled expression trees for performance.
/// </summary>
internal sealed class MediatoRHandlerCache : IMediatoRHandlerCache
{
    private readonly ConcurrentDictionary<Type, RequestHandlerWrapper> _requestHandlers = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<NotificationHandlerWrapper>> _notificationHandlers = new();

    /// <inheritdoc />
    public RequestHandlerWrapper GetRequestHandler(Type requestType, Type responseType)
    {
        if (_requestHandlers.TryGetValue(requestType, out var handler))
        {
            return handler;
        }

        throw new InvalidOperationException(
            $"No handler registered for request type '{requestType.Name}'. " +
            $"Ensure you've called AddCloudatR() with the assembly containing the handler.");
    }

    /// <inheritdoc />
    public IReadOnlyList<NotificationHandlerWrapper> GetNotificationHandlers(Type notificationType)
    {
        return _notificationHandlers.TryGetValue(notificationType, out var handlers)
            ? handlers
            : Array.Empty<NotificationHandlerWrapper>();
    }

    /// <summary>
    /// Caches a request handler during DI registration.
    /// </summary>
    internal void CacheRequestHandler(Type requestType, Type responseType)
    {
        if (_requestHandlers.ContainsKey(requestType))
        {
            // Handler already registered
            return;
        }

        // Use reflection to call the generic method
        var method = typeof(MediatoRHandlerCache)
            .GetMethod(nameof(CacheRequestHandlerGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(requestType, responseType);

        method.Invoke(this, null);
    }

    private void CacheRequestHandlerGeneric<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var requestType = typeof(TRequest);

        if (_requestHandlers.ContainsKey(requestType))
        {
            return;
        }

        // Build factory delegates with correct types
        var handlerFactory = BuildTypedHandlerFactory<TRequest, TResponse>();
        var behaviorFactory = BuildTypedBehaviorFactory<TRequest, TResponse>();
        var preProcessorFactory = BuildTypedPreProcessorFactory<TRequest>();
        var postProcessorFactory = BuildTypedPostProcessorFactory<TRequest, TResponse>();

        // Create the wrapper instance with correctly-typed delegates
        var wrapper = new RequestHandlerWrapperImpl<TRequest, TResponse>(
            handlerFactory,
            behaviorFactory,
            preProcessorFactory,
            postProcessorFactory);

        _requestHandlers[requestType] = wrapper;
    }

    /// <summary>
    /// Caches a notification handler during DI registration.
    /// </summary>
    internal void CacheNotificationHandler(Type notificationType, Type handlerInterfaceType)
    {
        var handlerFactory = BuildNotificationHandlerFactory(handlerInterfaceType);
        var wrapper = new NotificationHandlerWrapper(handlerFactory, notificationType);

        _notificationHandlers.AddOrUpdate(
            notificationType,
            new[] { wrapper },
            (_, existing) => existing.Append(wrapper).ToArray());
    }

    private static Func<IServiceProvider, IRequestHandler<TRequest, TResponse>> BuildTypedHandlerFactory<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        // Build expression: sp => sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var serviceType = typeof(IRequestHandler<TRequest, TResponse>);
        var getServiceMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(serviceType);

        var call = Expression.Call(null, getServiceMethod, serviceProviderParam);
        var lambda = Expression.Lambda<Func<IServiceProvider, IRequestHandler<TRequest, TResponse>>>(call, serviceProviderParam);

        return lambda.Compile();
    }

    private static Func<IServiceProvider, IEnumerable<IPipelineBehavior<TRequest, TResponse>>>? BuildTypedBehaviorFactory<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        // Build expression: sp => sp.GetServices<IPipelineBehavior<TRequest, TResponse>>()
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var behaviorType = typeof(IPipelineBehavior<TRequest, TResponse>);
        var getServicesMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetServices), new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(behaviorType);

        var call = Expression.Call(null, getServicesMethod, serviceProviderParam);
        var lambda = Expression.Lambda<Func<IServiceProvider, IEnumerable<IPipelineBehavior<TRequest, TResponse>>>>(call, serviceProviderParam);

        return lambda.Compile();
    }

    private static Func<IServiceProvider, IEnumerable<IRequestPreProcessor<TRequest>>>? BuildTypedPreProcessorFactory<TRequest>()
    {
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var preProcessorType = typeof(IRequestPreProcessor<TRequest>);
        var getServicesMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetServices), new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(preProcessorType);

        var call = Expression.Call(null, getServicesMethod, serviceProviderParam);
        var lambda = Expression.Lambda<Func<IServiceProvider, IEnumerable<IRequestPreProcessor<TRequest>>>>(call, serviceProviderParam);

        return lambda.Compile();
    }

    private static Func<IServiceProvider, IEnumerable<IRequestPostProcessor<TRequest, TResponse>>>? BuildTypedPostProcessorFactory<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var postProcessorType = typeof(IRequestPostProcessor<TRequest, TResponse>);
        var getServicesMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetServices), new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(postProcessorType);

        var call = Expression.Call(null, getServicesMethod, serviceProviderParam);
        var lambda = Expression.Lambda<Func<IServiceProvider, IEnumerable<IRequestPostProcessor<TRequest, TResponse>>>>(call, serviceProviderParam);

        return lambda.Compile();
    }

    private static Func<IServiceProvider, object> BuildNotificationHandlerFactory(Type serviceType)
    {
        // Build expression: sp => sp.GetRequiredService<TService>()
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "sp");
        var getServiceMethod = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(serviceType);

        var call = Expression.Call(null, getServiceMethod, serviceProviderParam);
        var cast = Expression.Convert(call, typeof(object));
        var lambda = Expression.Lambda<Func<IServiceProvider, object>>(cast, serviceProviderParam);

        return lambda.Compile();
    }
}
