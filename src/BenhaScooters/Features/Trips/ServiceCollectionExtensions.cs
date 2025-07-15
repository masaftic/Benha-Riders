using BenhaScooters.Features.Trips.Services;

namespace BenhaScooters.Features.Trips;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTripServices(this IServiceCollection services)
    {
        services.AddScoped<IFareEstimator, FareEstimator>();
        
        return services;
    }
}
