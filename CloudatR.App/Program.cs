using CloudatR.Lib;
using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;
using CloudatR.Lib.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register CloudatR with configuration
builder.Services.AddCloudatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<Program>();
    config.DefaultSource = "cloudatr-test-app";
    config.HandlerLifetime = ServiceLifetime.Transient;
});

var app = builder.Build();

var mediator = app.Services.GetRequiredService<IMediator>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("=== CloudatR Test Suite ===\n");

// Test 1: Basic Request/Response with internal classes
logger.LogInformation("Test 1: Basic Request/Response");
var weatherResult = await mediator.Send(new GetWeatherQuery { City = "Seattle" });
logger.LogInformation("Result: Weather in {City}: {Temperature}°F\n", weatherResult.City, weatherResult.Temperature);

// Test 2: Request with no return value (Unit)
logger.LogInformation("Test 2: Command with Unit return");
await mediator.Send(new UpdateSettingsCommand { SettingName = "Theme", Value = "Dark" });
logger.LogInformation("Settings updated successfully\n");

// Test 3: CloudEvent Context Access
logger.LogInformation("Test 3: CloudEvent Context Access");
var contextResult = await mediator.Send(new CloudEventAwareQuery { Data = "Test" });
logger.LogInformation("Result: {Result}\n", contextResult);

// Test 4: Custom CloudEvent Attributes
logger.LogInformation("Test 4: Custom CloudEvent Attributes");
var orderResult = await mediator.Send(new CreateOrderCommand { CustomerId = 123, Amount = 99.99m });
logger.LogInformation("Result: Order created with ID {OrderId}\n", orderResult.OrderId);

// Test 5: Notification with multiple handlers
logger.LogInformation("Test 5: Notification with multiple handlers (Async)");
await mediator.Publish(new OrderCreatedNotification { OrderId = 12345, Amount = 199.99m }, PublishStrategy.Async);
logger.LogInformation("");

// Test 6: Notification with SyncContinueOnException
logger.LogInformation("Test 6: Notification with SyncContinueOnException");
await mediator.Publish(new OrderCreatedNotification { OrderId = 67890, Amount = 299.99m });
logger.LogInformation("");

// Test 7: Performance benchmark
logger.LogInformation("Test 7: Performance Benchmark");
const int iterations = 100_000;
var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < iterations; i++)
{
    await mediator.Send(new FastQuery { Value = i });
}
stopwatch.Stop();
logger.LogInformation("{Iterations:N0} requests in {Elapsed}ms ({Rate:N0} req/sec)\n",
    iterations, stopwatch.ElapsedMilliseconds, iterations * 1000.0 / stopwatch.ElapsedMilliseconds);

logger.LogInformation("=== All Tests Completed Successfully! ===");

// Internal test types and handlers

// Test 1: Basic query
internal record GetWeatherQuery : IRequest<WeatherResult>
{
    public required string City { get; init; }
}

internal record WeatherResult
{
    public required string City { get; init; }
    public int Temperature { get; init; }
}

internal class GetWeatherHandler : IRequestHandler<GetWeatherQuery, WeatherResult>
{
    public Task<WeatherResult> Handle(GetWeatherQuery request, CancellationToken ct)
    {
        return Task.FromResult(new WeatherResult
        {
            City = request.City,
            Temperature = 72
        });
    }
}

// Test 2: Command with Unit return
internal record UpdateSettingsCommand : IRequest
{
    public required string SettingName { get; init; }
    public required string Value { get; init; }
}

internal class UpdateSettingsHandler : IRequestHandler<UpdateSettingsCommand, Unit>
{
    private readonly ILogger<UpdateSettingsHandler> _logger;

    public UpdateSettingsHandler(ILogger<UpdateSettingsHandler> logger)
    {
        _logger = logger;
    }

    public Task<Unit> Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Updated {Setting} to {Value}", request.SettingName, request.Value);
        return Task.FromResult(Unit.Value);
    }
}

// Test 3: CloudEvent context access
internal record CloudEventAwareQuery : IRequest<string>
{
    public required string Data { get; init; }
}

internal class CloudEventAwareHandler : IRequestHandler<CloudEventAwareQuery, string>
{
    private readonly ICloudEventContext _context;
    private readonly ILogger<CloudEventAwareHandler> _logger;

    public CloudEventAwareHandler(ICloudEventContext context, ILogger<CloudEventAwareHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<string> Handle(CloudEventAwareQuery request, CancellationToken ct)
    {
        _logger.LogInformation("CloudEvent ID: {EventId}", _context.EventId);
        _logger.LogInformation("CloudEvent Type: {Type}", _context.Type);
        _logger.LogInformation("CloudEvent Source: {Source}", _context.Source);
        _logger.LogInformation("CloudEvent Time: {Time}", _context.Time);

        if (_context.Extensions.CorrelationId != null)
        {
            _logger.LogInformation("Correlation ID: {CorrelationId}", _context.Extensions.CorrelationId);
        }

        return Task.FromResult($"Processed with CloudEvent ID: {_context.EventId}");
    }
}

// Test 4: Custom CloudEvent attributes
[CloudEvent(Type = "com.example.order.create", Source = "order-service", Subject = "/orders")]
internal record CreateOrderCommand : IRequest<OrderCreatedResult>
{
    public int CustomerId { get; init; }
    public decimal Amount { get; init; }
}

internal record OrderCreatedResult
{
    public int OrderId { get; init; }
}

internal class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderCreatedResult>
{
    private readonly ICloudEventContext _context;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(ICloudEventContext context, ILogger<CreateOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<OrderCreatedResult> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating order for customer {CustomerId} with custom CloudEvent type: {Type}",
            request.CustomerId, _context.Type);

        return Task.FromResult(new OrderCreatedResult { OrderId = Random.Shared.Next(1000, 9999) });
    }
}

// Test 5 & 6: Notifications
internal record OrderCreatedNotification : INotification
{
    public int OrderId { get; init; }
    public decimal Amount { get; init; }
}

internal class EmailNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<EmailNotificationHandler> _logger;

    public EmailNotificationHandler(ILogger<EmailNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate work
        _logger.LogInformation("[Email] Sending confirmation email for order {OrderId}", notification.OrderId);
    }
}

internal class InventoryNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<InventoryNotificationHandler> _logger;

    public InventoryNotificationHandler(ILogger<InventoryNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate work
        _logger.LogInformation("[Inventory] Updating inventory for order {OrderId}", notification.OrderId);
    }
}

internal class AnalyticsNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<AnalyticsNotificationHandler> _logger;

    public AnalyticsNotificationHandler(ILogger<AnalyticsNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await Task.Delay(10, ct); // Simulate work
        _logger.LogInformation("[Analytics] Recording order {OrderId} amount ${Amount}",
            notification.OrderId, notification.Amount);
    }
}

// Test 7: Performance test
internal record FastQuery : IRequest<int>
{
    public int Value { get; init; }
}

internal class FastHandler : IRequestHandler<FastQuery, int>
{
    public Task<int> Handle(FastQuery request, CancellationToken ct)
    {
        return Task.FromResult(request.Value * 2);
    }
}

// Pipeline behaviors (commented out for MVP - open generic behaviors require special registration)
// TODO: Implement proper open generic behavior support
//internal class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
//    where TRequest : IRequest<TResponse>
//{
//    ...
//}
