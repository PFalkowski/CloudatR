using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Internal;

/// <summary>
/// Concrete implementation of request handler wrapper with pipeline support.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper
    where TRequest : IRequest<TResponse>
{
    private readonly Func<IServiceProvider, IRequestHandler<TRequest, TResponse>> _handlerFactory;
    private readonly Func<IServiceProvider, IEnumerable<IPipelineBehavior<TRequest, TResponse>>>? _behaviorFactory;
    private readonly Func<IServiceProvider, IEnumerable<IRequestPreProcessor<TRequest>>>? _preProcessorFactory;
    private readonly Func<IServiceProvider, IEnumerable<IRequestPostProcessor<TRequest, TResponse>>>? _postProcessorFactory;

    public RequestHandlerWrapperImpl(
        Func<IServiceProvider, IRequestHandler<TRequest, TResponse>> handlerFactory,
        Func<IServiceProvider, IEnumerable<IPipelineBehavior<TRequest, TResponse>>>? behaviorFactory,
        Func<IServiceProvider, IEnumerable<IRequestPreProcessor<TRequest>>>? preProcessorFactory,
        Func<IServiceProvider, IEnumerable<IRequestPostProcessor<TRequest, TResponse>>>? postProcessorFactory)
    {
        _handlerFactory = handlerFactory;
        _behaviorFactory = behaviorFactory;
        _preProcessorFactory = preProcessorFactory;
        _postProcessorFactory = postProcessorFactory;
    }

    public override async Task<TResp> Handle<TResp>(
        IRequest<TResp> request,
        ICloudEventContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        // Update the scoped CloudEvent context
        await UpdateCloudEventContext(serviceProvider, context);

        // Execute pre-processors
        if (_preProcessorFactory != null)
        {
            var preProcessors = _preProcessorFactory(serviceProvider);
            foreach (var preProcessor in preProcessors)
            {
                await preProcessor.Process(typedRequest, cancellationToken);
            }
        }

        TResponse response;

        // Build and execute pipeline
        if (_behaviorFactory == null)
        {
            // No behaviors, execute handler directly
            response = await _handlerFactory(serviceProvider).Handle(typedRequest, cancellationToken);
        }
        else
        {
            // Build pipeline with behaviors
            var behaviors = _behaviorFactory(serviceProvider).Reverse().ToList();

            RequestHandlerDelegate<TResponse> pipeline = () =>
                _handlerFactory(serviceProvider).Handle(typedRequest, cancellationToken);

            foreach (var behavior in behaviors)
            {
                var next = pipeline;
                pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
            }

            response = await pipeline();
        }

        // Execute post-processors
        if (_postProcessorFactory != null)
        {
            var postProcessors = _postProcessorFactory(serviceProvider);
            foreach (var postProcessor in postProcessors)
            {
                await postProcessor.Process(typedRequest, response, cancellationToken);
            }
        }

        return (TResp)(object)response!;
    }

    private static Task UpdateCloudEventContext(IServiceProvider serviceProvider, ICloudEventContext context)
    {
        // Get the scoped CloudEventContext and update it
        var scopedContext = serviceProvider.GetService<CloudEventContext>();
        if (scopedContext != null)
        {
            // Update the scoped context with values from the created context
            scopedContext.EventId = context.EventId;
            scopedContext.Source = context.Source;
            scopedContext.Type = context.Type;
            scopedContext.Time = context.Time;
            scopedContext.Subject = context.Subject;
            scopedContext.Extensions = context.Extensions;
        }

        return Task.CompletedTask;
    }
}
