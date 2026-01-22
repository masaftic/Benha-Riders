using System.Reflection;
using BenhaScooters.Application.Common.Behaviors;
using BenhaScooters.Application.Features.Matching.Services;
using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Application.Services;
using BenhaScooters.Domain.Matching;
using FluentValidation;

namespace BenhaScooters.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
            // config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            // config.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddOptions<MatchingSessionOptions>()
            .Bind(configuration.GetSection(MatchingSessionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register application services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IDriverMatchingService, DriverMatchingService>();
        services.AddScoped<IMatchingOrchestrator, MatchingOrchestrator>();

        return services;
    }
}
