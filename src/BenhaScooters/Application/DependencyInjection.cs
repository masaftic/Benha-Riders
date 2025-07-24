using System.Reflection;
using BenhaScooters.Application.Common.Behaviors;
using BenhaScooters.Application.Services;
using FluentValidation;
using MediatR;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace BenhaScooters.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(DomainEventDispatcherBehavior<,>));
            // config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            // config.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register application services
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}
