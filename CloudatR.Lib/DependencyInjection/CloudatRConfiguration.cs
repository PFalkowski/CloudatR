using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CloudatR.Lib.DependencyInjection;

/// <summary>
/// Configuration for CloudatR registration.
/// </summary>
public sealed class CloudatRConfiguration
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<Type> GlobalBehaviors { get; } = new();

    /// <summary>
    /// The service lifetime for handlers. Default is Transient.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// The default source for CloudEvents when not specified via attribute.
    /// </summary>
    public string DefaultSource { get; set; } = "cloudator";

    /// <summary>
    /// Custom type naming convention for CloudEvent type generation.
    /// If null, uses the default convention.
    /// </summary>
    public Func<Type, string>? TypeNameConvention { get; set; }

    /// <summary>
    /// Registers handlers and related services from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public CloudatRConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        Assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers handlers and related services from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <returns>This configuration instance for chaining.</returns>
    public CloudatRConfiguration RegisterServicesFromAssemblyContaining<T>()
    {
        return RegisterServicesFromAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// Registers handlers and related services from the assembly containing the specified type.
    /// </summary>
    /// <param name="type">A type in the assembly to scan.</param>
    /// <returns>This configuration instance for chaining.</returns>
    public CloudatRConfiguration RegisterServicesFromAssemblyContaining(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return RegisterServicesFromAssembly(type.Assembly);
    }

    /// <summary>
    /// Adds an open generic pipeline behavior that will be applied to all requests.
    /// </summary>
    /// <param name="behaviorType">The open generic behavior type (e.g., typeof(LoggingBehavior&lt;,&gt;)).</param>
    /// <returns>This configuration instance for chaining.</returns>
    public CloudatRConfiguration AddOpenBehavior(Type behaviorType)
    {
        if (behaviorType == null)
        {
            throw new ArgumentNullException(nameof(behaviorType));
        }

        if (!behaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException("Behavior type must be an open generic type.", nameof(behaviorType));
        }

        GlobalBehaviors.Add(behaviorType);
        return this;
    }

    /// <summary>
    /// Adds a closed generic or non-generic pipeline behavior.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type.</typeparam>
    /// <returns>This configuration instance for chaining.</returns>
    public CloudatRConfiguration AddBehavior<TBehavior>()
        where TBehavior : class
    {
        GlobalBehaviors.Add(typeof(TBehavior));
        return this;
    }
}
