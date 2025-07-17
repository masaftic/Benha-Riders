using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BenhaScooters.Presentation;

public class EndpointConfiguration
{
    public string? Prefix { get; set; } = "/api";
}

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        return AddEndpoints(services, Assembly.GetExecutingAssembly());
    }

    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);
        return services;
    }

    public static IApplicationBuilder MapEndpoints(this WebApplication app, Action<EndpointConfiguration>? configure = null)
    {
        IEnumerable<IEndpoint> endpoints = app.Services.GetServices<IEndpoint>();

        var config = new EndpointConfiguration();
        configure?.Invoke(config);

        IEndpointRouteBuilder routeBuilder = app;
        if (!string.IsNullOrEmpty(config.Prefix))
        {
            routeBuilder = routeBuilder.MapGroup(config.Prefix);
        }

        foreach (IEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(routeBuilder);
        }

        return app;
    }
}