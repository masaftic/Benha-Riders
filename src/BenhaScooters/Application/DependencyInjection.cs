using System.Reflection;
using BenhaScooters.Application.Common.Behaviors;
using BenhaScooters.Application.Common.Settings;
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
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddOptions<MatchingSessionOptions>()
            .Bind(configuration.GetSection(MatchingSessionOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.OffersPerRound?.Length == options.NumberOfRounds,
                "OffersPerRound length must equal NumberOfRounds")
            .ValidateOnStart();
        
        services.AddOptions<DriverWalletOptions>()
            .Bind(configuration.GetSection(DriverWalletOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register application services
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}
