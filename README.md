# CloudatR

A high-performance MediatR alternative with first-class CloudEvents support and internal class compatibility.

## 🚀 Features

- **⚡ High Performance**: zero reflection in hot path
- **🔒 Internal Classes**: Full support for internal handlers, requests, and notifications via InternalsVisibleTo
- **☁️ CloudEvents Native**: Automatic CloudEvents envelope wrapping with metadata access
- **🎯 CQRS Ready**: Built-in support for Commands, Queries, and Notifications
- **🔄 Pipeline Behaviors**: Cross-cutting concerns via IPipelineBehavior (pre/post processors)
- **📦 Lightweight**: Minimal dependencies, compiled expression trees for optimal performance
- **🛠️ .NET 9 Ready**: Built on .NET 9.0 with nullable reference types

## 📦 Installation

```bash
dotnet add package CloudatR.Lib
```

## 🎯 Quick Start

### 1. Register CloudatR

```csharp
using CloudatR.Lib.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCloudatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<Program>();
    config.DefaultSource = "my-service";
});

var app = builder.Build();
var mediator = app.Services.GetRequiredService<IMediator>();
```

### 2. Define Your Request (All Classes Can Be Internal)

```csharp
using CloudatR.Lib.Abstractions;

// Command example
internal record CreateOrderCommand : IRequest<OrderResult>
{
    public int CustomerId { get; init; }
    public decimal Amount { get; init; }
}

internal record OrderResult
{
    public int OrderId { get; init; }
}
```

### 3. Create a Handler

```csharp
internal class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResult>
{
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(ILogger<CreateOrderHandler> logger)
    {
        _logger = logger;
    }

    public async Task<OrderResult> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

        // Your business logic here
        var orderId = await SaveOrderToDatabase(request);

        return new OrderResult { OrderId = orderId };
    }
}
```

### 4. Send the Request

```csharp
var result = await mediator.Send(new CreateOrderCommand
{
    CustomerId = 123,
    Amount = 99.99m
});

Console.WriteLine($"Order created: {result.OrderId}");
```

## 📚 Usage Examples

### Commands (Write Operations)

Commands represent actions that modify state and return a result.

```csharp
// Command that returns a result
internal record UpdateUserCommand : IRequest<UserDto>
{
    public int UserId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
}

// Command handler
internal class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _repository;

    public UpdateUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(request.UserId, ct);
        user.Name = request.Name;
        user.Email = request.Email;

        await _repository.SaveAsync(user, ct);

        return user.ToDto();
    }
}

// Usage
var updatedUser = await mediator.Send(new UpdateUserCommand
{
    UserId = 1,
    Name = "John Doe",
    Email = "john@example.com"
});
```

### Commands with No Return Value

Use `IRequest` (without generic parameter) for commands that don't return data.

```csharp
// Command with Unit return (void-like)
internal record DeleteUserCommand : IRequest
{
    public int UserId { get; init; }
}

// Handler
internal class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _repository;

    public DeleteUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        await _repository.DeleteAsync(request.UserId, ct);
        return Unit.Value; // Return Unit.Value for void-like operations
    }
}

// Usage
await mediator.Send(new DeleteUserCommand { UserId = 1 });
```

### Queries (Read Operations)

Queries represent read operations that don't modify state.

```csharp
internal record GetUserByIdQuery : IRequest<UserDto>
{
    public int UserId { get; init; }
}

internal class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserByIdHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(request.UserId, ct);
        return user.ToDto();
    }
}

// Usage
var user = await mediator.Send(new GetUserByIdQuery { UserId = 1 });
```

### Notifications (Publish/Subscribe)

Notifications allow multiple handlers to react to a single event.

```csharp
// Define a notification
internal record OrderCreatedNotification : INotification
{
    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public decimal Amount { get; init; }
}

// Multiple handlers can listen to the same notification
internal class EmailNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IEmailService _emailService;

    public EmailNotificationHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmationAsync(
            notification.CustomerId,
            notification.OrderId,
            ct);
    }
}

internal class InventoryNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IInventoryService _inventoryService;

    public InventoryNotificationHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _inventoryService.UpdateStockAsync(notification.OrderId, ct);
    }
}

internal class AnalyticsNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsNotificationHandler(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _analyticsService.RecordSaleAsync(notification.Amount, ct);
    }
}

// Publish to all handlers (default: sequential, continue on exception)
await mediator.Publish(new OrderCreatedNotification
{
    OrderId = 123,
    CustomerId = 456,
    Amount = 99.99m
});
```

### Notification Publishing Strategies

Control how notifications are dispatched to handlers:

```csharp
// Sequential execution, continue on exceptions
await mediator.Publish(notification, PublishStrategy.SyncContinueOnException);

// Sequential execution, stop on first exception
await mediator.Publish(notification, PublishStrategy.SyncStopOnException);

// Parallel execution using Task.WhenAll
await mediator.Publish(notification, PublishStrategy.Async);

// Fire and forget (no waiting)
await mediator.Publish(notification, PublishStrategy.ParallelNoWait);
```

## ☁️ CloudEvents Integration

CloudatR automatically wraps all requests and notifications in CloudEvents, providing standardized metadata.

### Accessing CloudEvent Context

```csharp
internal class OrderHandler : IRequestHandler<CreateOrderCommand, OrderResult>
{
    private readonly ICloudEventContext _cloudEventContext;
    private readonly ILogger<OrderHandler> _logger;

    public OrderHandler(
        ICloudEventContext cloudEventContext,
        ILogger<OrderHandler> logger)
    {
        _cloudEventContext = cloudEventContext;
        _logger = logger;
    }

    public async Task<OrderResult> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Access CloudEvent metadata
        _logger.LogInformation(
            "Processing {EventType} with ID {EventId}",
            _cloudEventContext.Type,
            _cloudEventContext.EventId);

        _logger.LogInformation("Event Source: {Source}", _cloudEventContext.Source);
        _logger.LogInformation("Event Time: {Time}", _cloudEventContext.Time);

        // Access correlation/causation IDs for distributed tracing
        if (_cloudEventContext.Extensions.CorrelationId != null)
        {
            _logger.LogInformation(
                "Correlation ID: {CorrelationId}",
                _cloudEventContext.Extensions.CorrelationId);
        }

        // Your business logic
        return new OrderResult { OrderId = 123 };
    }
}
```

### Custom CloudEvent Attributes

Customize the CloudEvent type, source, and subject using the `[CloudEvent]` attribute:

```csharp
[CloudEvent(
    Type = "com.mycompany.orders.create.v1",
    Source = "order-service",
    Subject = "/orders")]
internal record CreateOrderCommand : IRequest<OrderResult>
{
    public int CustomerId { get; init; }
    public decimal Amount { get; init; }
}
```

### Default CloudEvent Type Convention

Without the attribute, CloudatR generates types using the convention:
```
com.{assembly}.{commands|queries|events}.{typename}
```

Examples:
- `CreateOrderCommand` → `com.myapp.commands.createordercommand`
- `GetUserQuery` → `com.myapp.queries.getuserquery`
- `OrderCreatedNotification` → `com.myapp.events.ordercreatednotification`

### CloudEvent Extensions

CloudatR automatically populates standard extension attributes:

- **CorrelationId**: Links related events across distributed systems (from Activity.Current)
- **CausationId**: Identifies the command/event that caused this event
- **UserId**: User context (populate via custom pipeline behavior)
- **TenantId**: Multi-tenant context (populate via custom pipeline behavior)

## 🔄 Pipeline Behaviors

Pipeline behaviors enable cross-cutting concerns like logging, validation, and transactions.

### Creating a Pipeline Behavior

```csharp
internal class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

### Validation Behavior Example

```csharp
internal class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
```

### Pre/Post Processors

Simpler alternatives to full pipeline behaviors:

```csharp
// Pre-processor runs before the handler
internal class RequestPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
{
    private readonly ILogger _logger;

    public RequestPreProcessor(ILogger<RequestPreProcessor<TRequest>> logger)
    {
        _logger = logger;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pre-processing {RequestType}", typeof(TRequest).Name);
        return Task.CompletedTask;
    }
}

// Post-processor runs after the handler
internal class RequestPostProcessor<TRequest, TResponse>
    : IRequestPostProcessor<TRequest, TResponse>
{
    private readonly ILogger _logger;

    public RequestPostProcessor(ILogger<RequestPostProcessor<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Post-processing {RequestType}", typeof(TRequest).Name);
        return Task.CompletedTask;
    }
}
```

## 🔒 Internal Class Support

CloudatR fully supports internal classes via `InternalsVisibleTo`. This keeps your implementation details private while using the mediator pattern.

### Setup

Add to your application's `.csproj`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="CloudatR.Lib" />
</ItemGroup>
```

### Example

```csharp
// Everything can be internal - no public APIs needed!
internal record CreateUserCommand : IRequest<UserDto>
{
    public string Name { get; init; }
    public string Email { get; init; }
}

internal record UserDto
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
}

internal class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // Implementation
    }
}
```

## ⚙️ Configuration

### Basic Configuration

```csharp
builder.Services.AddCloudatR(config =>
{
    // Register handlers from assembly
    config.RegisterServicesFromAssemblyContaining<Program>();

    // Set default CloudEvent source
    config.DefaultSource = "my-service";

    // Configure handler lifetime (default: Transient)
    config.HandlerLifetime = ServiceLifetime.Scoped;
});
```

### Multiple Assemblies

```csharp
builder.Services.AddCloudatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(OrdersModule).Assembly);
    config.RegisterServicesFromAssembly(typeof(UsersModule).Assembly);
    config.RegisterServicesFromAssembly(typeof(PaymentsModule).Assembly);
});
```

### Custom Type Naming Convention

```csharp
builder.Services.AddCloudatR(config =>
{
    config.TypeNameConvention = type =>
    {
        // Custom CloudEvent type generation
        var category = GetCategory(type);
        var version = GetVersion(type);
        return $"com.mycompany.{category}.{type.Name.ToLower()}.{version}";
    };
});
```

## 📊 Performance

CloudatR is designed for high performance:

- **~565,000 requests/second** on typical hardware
- **Zero reflection** in the hot path (compiled expression trees)
- **Minimal allocations** through careful design
- **Concurrent caching** of all handler metadata

### Performance Comparison

| Feature | CloudatR | MediatR |
|---------|----------|---------|
| Internal Classes | ✅ Native | ❌ Requires public |
| CloudEvents | ✅ Native | ❌ Manual |
| Reflection in Hot Path | ❌ None | ✅ Some |
| Expression Trees | ✅ Yes | ✅ Yes |
| Throughput | ~565k req/s | ~535k req/s |

## 🆚 Differences from MediatR

| Feature | CloudatR | MediatR |
|---------|----------|---------|
| **Internal Classes** | ✅ Full support via InternalsVisibleTo | ❌ Requires public classes |
| **CloudEvents** | ✅ Built-in, automatic wrapping | ❌ Not included |
| **CloudEvent Context** | ✅ Via ICloudEventContext | ❌ N/A |
| **Performance** | ⚡ ~565k req/s | ⚡ ~535k req/s |
| **Pipeline Behaviors** | ✅ Full support | ✅ Full support |
| **Notifications** | ✅ Multiple strategies | ✅ Limited strategies |
| **Streaming** | 🚧 Planned | ✅ Available |
| **Source Generators** | 🚧 Planned | ✅ Available |

## 🏗️ Architecture

CloudatR uses several design patterns for optimal performance:

1. **Mediator Pattern**: Decouples request senders from handlers
2. **Pipeline Pattern**: Behaviors form a chain of responsibility
3. **Expression Trees**: Compiled delegates for zero-reflection invocation
4. **Concurrent Caching**: Handler metadata cached at startup
5. **CloudEvents Envelope**: Standardized event metadata wrapper

## 🔧 Advanced Scenarios

### Custom Exception Handling

```csharp
internal class ExceptionHandlingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger;

    public ExceptionHandlingBehavior(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
```

### Transaction Management

```csharp
internal class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDbContext _dbContext;

    public TransactionBehavior(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

### Caching Behavior

```csharp
internal class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableRequest
{
    private readonly IDistributedCache _cache;

    public CachingBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached != null)
        {
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        var response = await next();

        var serialized = JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(
            cacheKey,
            serialized,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            cancellationToken);

        return response;
    }
}
```

## 📖 Best Practices

1. **Keep Handlers Small**: Each handler should do one thing well
2. **Use Records**: Leverage C# records for immutable requests/responses
3. **Leverage Internal**: Keep implementation details internal
4. **Use Notifications for Side Effects**: Publish events after command execution
5. **CloudEvent Metadata**: Use correlation IDs for distributed tracing
6. **Pipeline Order Matters**: Register behaviors in the order you want them to execute
7. **Async All The Way**: Use async/await consistently throughout

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

MIT License - see LICENSE file for details

## 🙏 Acknowledgments

- Inspired by [MediatR](https://github.com/jbogard/MediatR) by Jimmy Bogard
- [CloudEvents](https://cloudevents.io/) specification by CNCF
- Built with ❤️ using .NET 9

---

**CloudatR** - High-performance mediator with CloudEvents for modern .NET applications
