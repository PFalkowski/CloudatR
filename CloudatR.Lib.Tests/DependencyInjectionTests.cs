using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddCloudatR_RegistersCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCloudatR();
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        var configuration = provider.GetService<CloudatRConfiguration>();

        mediator.Should().NotBeNull();
        configuration.Should().NotBeNull();

        provider.Dispose();
    }

    [Fact]
    public void AddCloudatR_RegistersCloudEventContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCloudatR();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetService<ICloudEventContext>();
        context.Should().NotBeNull();

        provider.Dispose();
    }

    [Fact]
    public async Task AddCloudatR_WithAssembly_RegistersHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCloudatR(typeof(TestRequest).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Assert
        var request = new TestRequest("test");
        var response = await mediator.Send(request);
        response.Result.Should().Be("Handled: test");

        provider.Dispose();
    }

    [Fact]
    public void AddCloudatR_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configWasCalled = false;

        // Act
        services.AddCloudatR(config =>
        {
            configWasCalled = true;
            config.DefaultSource = "test-source";
            config.HandlerLifetime = ServiceLifetime.Scoped;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        configWasCalled.Should().BeTrue();
        var configuration = provider.GetRequiredService<CloudatRConfiguration>();
        configuration.DefaultSource.Should().Be("test-source");
        configuration.HandlerLifetime.Should().Be(ServiceLifetime.Scoped);

        provider.Dispose();
    }

    [Fact]
    public void AddCloudatR_RegistersMultipleHandlersForSameNotification()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCloudatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<TestNotification>();
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var handler1 = provider.GetService<INotificationHandler<TestNotification>>();
        handler1.Should().NotBeNull();

        provider.Dispose();
    }

    [Fact]
    public void Configuration_RegisterServicesFromAssembly_ReturnsConfiguration()
    {
        // Arrange
        var config = new CloudatRConfiguration();
        var assembly = typeof(TestRequest).Assembly;

        // Act
        var result = config.RegisterServicesFromAssembly(assembly);

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void Configuration_RegisterServicesFromAssembly_WithNull_ThrowsException()
    {
        // Arrange
        var config = new CloudatRConfiguration();

        // Act
        Action act = () => config.RegisterServicesFromAssembly(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Configuration_RegisterServicesFromAssemblyContaining_ReturnsConfiguration()
    {
        // Arrange
        var config = new CloudatRConfiguration();

        // Act
        var result = config.RegisterServicesFromAssemblyContaining<TestRequest>();

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void Configuration_RegisterServicesFromAssemblyContaining_WithType_ReturnsConfiguration()
    {
        // Arrange
        var config = new CloudatRConfiguration();

        // Act
        var result = config.RegisterServicesFromAssemblyContaining(typeof(TestRequest));

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void Configuration_RegisterServicesFromAssemblyContaining_WithNullType_ThrowsException()
    {
        // Arrange
        var config = new CloudatRConfiguration();

        // Act
        Action act = () => config.RegisterServicesFromAssemblyContaining((Type)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Configuration_AddOpenBehavior_ReturnsConfiguration()
    {
        // Arrange
        var config = new CloudatRConfiguration();
        var behaviorType = typeof(LoggingBehavior<,>);

        // Act
        var result = config.AddOpenBehavior(behaviorType);

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void Configuration_AddOpenBehavior_WithNonGeneric_ThrowsException()
    {
        // Arrange
        var config = new CloudatRConfiguration();

        // Act
        Action act = () => config.AddOpenBehavior(typeof(string));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*open generic type*");
    }

    [Fact]
    public void Configuration_AddOpenBehavior_WithNull_ThrowsException()
    {
        // Arrange
        var config = new CloudatRConfiguration();

        // Act
        Action act = () => config.AddOpenBehavior(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Configuration_AddBehavior_ReturnsConfiguration()
    {
        // Arrange
        var config = new CloudatRConfiguration();

        // Act
        var result = config.AddBehavior<ValidationBehavior<TestRequest, TestResponse>>();

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void Configuration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new CloudatRConfiguration();

        // Assert
        config.HandlerLifetime.Should().Be(ServiceLifetime.Transient);
        config.DefaultSource.Should().Be("cloudator");
        config.TypeNameConvention.Should().BeNull();
    }

    [Fact]
    public async Task AddCloudatR_WithMultipleAssemblies_RegistersAllHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly1 = typeof(TestRequest).Assembly;
        var assembly2 = typeof(TestNotification).Assembly;

        // Act
        services.AddCloudatR(assembly1, assembly2);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Assert
        var request = new TestRequest("test");
        var response = await mediator.Send(request);
        response.Should().NotBeNull();

        provider.Dispose();
    }

    [Fact]
    public void AddCloudatR_RegistersMediatorAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCloudatR();

        // Act
        var provider = services.BuildServiceProvider();
        var mediator1 = provider.GetRequiredService<IMediator>();
        var mediator2 = provider.GetRequiredService<IMediator>();

        // Assert
        mediator1.Should().NotBeSameAs(mediator2);

        provider.Dispose();
    }
}
