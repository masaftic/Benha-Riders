using BenhaScooters.Contracts.GoogleMaps;
using ErrorOr;

namespace BenhaScooters.Application.Abstractions;

public interface IGoogleMapsService
{
    /// <summary>
    /// Reverse geocode coordinates to get the formatted address
    /// </summary>
    Task<ErrorOr<ReverseGeocodeResponse>> ReverseGeocodeAsync(
        double latitude, 
        double longitude, 
        string language = "ar",
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get autocomplete predictions for a search query
    /// </summary>
    Task<ErrorOr<AutocompleteResponse>> AutocompleteAsync(
        string input,
        string language = "ar",
        double? latitude = null,
        double? longitude = null,
        string? components = "country:eg",
        int radius = 50000,
        string types = "geocode|establishment",
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get details for a specific place
    /// </summary>
    Task<ErrorOr<PlaceDetailsResponse>> GetPlaceDetailsAsync(
        string placeId,
        string language = "ar",
        string fields = "name,geometry",
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get directions between two points
    /// </summary>
    Task<ErrorOr<DirectionsResponse>> GetDirectionsAsync(
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude,
        string language = "ar",
        string mode = "driving",
        bool alternatives = false,
        CancellationToken cancellationToken = default);
}
