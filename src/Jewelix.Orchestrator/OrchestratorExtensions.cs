using Jewelix.Orchestrator.Abstractions;

namespace Jewelix.Orchestrator;

/// <summary>
/// DI registration for the Jewelix CQRS library.
/// </summary>
public static class OrchestratorExtensions
{
    private static readonly Type CommandHandlerOpenType = typeof(ICommandHandler<,>);
    private static readonly Type QueryHandlerOpenType = typeof(IQueryHandler<,>);
    private static readonly Type EventHandlerOpenType = typeof(IEventHandler<>);

    /// <summary>
    /// Registers <see cref="IOrchestrator"/> and scans <paramref name="assemblies"/> for all
    /// <see cref="ICommandHandler{TCommand,TResult}"/>, <see cref="IQueryHandler{TQuery,TResult}"/>,
    /// and <see cref="IEventHandler{TEvent}"/> implementations, registering each as transient.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="assemblies">
    /// Assemblies to scan for handler implementations. Pass the assembly containing your handlers,
    /// e.g. <c>typeof(MyHandler).Assembly</c>.
    /// </param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="assemblies"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddJewelixOrchestrator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddSingleton<IOrchestrator, Orchestrator>();

        foreach (var assembly in assemblies)
        {
            ScanAndRegister(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Adds an open-generic <see cref="IPipelineBehavior{TInput,TOutput}"/> to the pipeline.
    /// Behaviors are applied in registration order; first registered wraps outermost.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="openGenericBehaviorType">
    /// An open generic type implementing <see cref="IPipelineBehavior{TInput,TOutput}"/>,
    /// e.g. <c>typeof(LoggingBehavior&lt;,&gt;)</c>.
    /// </param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddJewelixOrchestratorBehavior(
        this IServiceCollection services,
        Type openGenericBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(openGenericBehaviorType);

        services.AddTransient(typeof(IPipelineBehavior<,>), openGenericBehaviorType);
        return services;
    }

    private static void ScanAndRegister(IServiceCollection services, Assembly assembly)
    {
        // IsPublic excludes nested types (private or internal test helpers, etc.).
        // Handlers must be top-level public classes per Clean Architecture conventions.
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false, IsPublic: true });

        foreach (var type in handlerTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                var definition = iface.GetGenericTypeDefinition();

                if (definition == CommandHandlerOpenType
                    || definition == QueryHandlerOpenType
                    || definition == EventHandlerOpenType)
                {
                    services.AddTransient(iface, type);
                }
            }
        }
    }
}
