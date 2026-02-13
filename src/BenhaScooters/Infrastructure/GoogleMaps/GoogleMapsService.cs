using System.Net.Http.Json;
using System.Text.Json;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Contracts.GoogleMaps;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.GoogleMaps;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleMapsOptions _options;
    private readonly ILogger<GoogleMapsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GoogleMapsService(
        HttpClient httpClient,
        IOptions<GoogleMapsOptions> options,
        ILogger<GoogleMapsService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ErrorOr<ReverseGeocodeResponse>> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        string language = "ar",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var latLng = $"{latitude},{longitude}";
            var url = $"{_options.BaseUrl}/geocode/json?latlng={latLng}&language={language}&key={_options.ApiKey}";

            _logger.LogDebug("Calling Google Geocode API for coordinates: {LatLng}", latLng);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<GoogleGeocodeApiResponse>(_jsonOptions, cancellationToken);

            if (apiResponse is null)
            {
                return Error.Failure("GoogleMaps.Geocode.NullResponse", "Received null response from Google Maps API");
            }

            var formattedAddress = apiResponse.Results?.FirstOrDefault()?.FormattedAddress;

            return new ReverseGeocodeResponse(
                formattedAddress,
                apiResponse.Status,
                apiResponse.ErrorMessage
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling Google Geocode API");
            return Error.Failure("GoogleMaps.Geocode.HttpError", $"Failed to call Google Maps API: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling Google Geocode API");
            return Error.Unexpected("GoogleMaps.Geocode.UnexpectedError", ex.Message);
        }
    }

    public async Task<ErrorOr<AutocompleteResponse>> AutocompleteAsync(
        string input,
        string language = "ar",
        double? latitude = null,
        double? longitude = null,
        string? components = "country:eg",
        int radius = 50000,
        string types = "geocode|establishment",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.BaseUrl}/place/autocomplete/json" +
                      $"?input={Uri.EscapeDataString(input)}" +
                      $"&language={language}" +
                      $"&radius={radius}" +
                      $"&types={Uri.EscapeDataString(types)}" +
                      $"&key={_options.ApiKey}";

            if (latitude.HasValue && longitude.HasValue)
            {
                url += $"&location={latitude},{longitude}";
            }

            if (!string.IsNullOrEmpty(components))
            {
                url += $"&components={Uri.EscapeDataString(components)}";
            }

            _logger.LogDebug("Calling Google Autocomplete API for input: {Input}", input);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<GoogleAutocompleteApiResponse>(_jsonOptions, cancellationToken);

            if (apiResponse is null)
            {
                return Error.Failure("GoogleMaps.Autocomplete.NullResponse", "Received null response from Google Maps API");
            }

            var predictions = apiResponse.Predictions?.Select(p => new AutocompletePrediction(
                p.Description,
                p.PlaceId,
                p.Reference,
                p.StructuredFormatting is not null
                    ? new StructuredFormatting(p.StructuredFormatting.MainText, p.StructuredFormatting.SecondaryText)
                    : null,
                p.Types
            )).ToList() ?? new List<AutocompletePrediction>();

            return new AutocompleteResponse(
                predictions,
                apiResponse.Status,
                apiResponse.ErrorMessage
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling Google Autocomplete API");
            return Error.Failure("GoogleMaps.Autocomplete.HttpError", $"Failed to call Google Maps API: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling Google Autocomplete API");
            return Error.Unexpected("GoogleMaps.Autocomplete.UnexpectedError", ex.Message);
        }
    }

    public async Task<ErrorOr<PlaceDetailsResponse>> GetPlaceDetailsAsync(
        string placeId,
        string language = "ar",
        string fields = "name,geometry",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.BaseUrl}/place/details/json" +
                      $"?place_id={Uri.EscapeDataString(placeId)}" +
                      $"&language={language}" +
                      $"&fields={Uri.EscapeDataString(fields)}" +
                      $"&key={_options.ApiKey}";

            _logger.LogDebug("Calling Google Place Details API for placeId: {PlaceId}", placeId);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<GooglePlaceDetailsApiResponse>(_jsonOptions, cancellationToken);

            if (apiResponse is null)
            {
                return Error.Failure("GoogleMaps.PlaceDetails.NullResponse", "Received null response from Google Maps API");
            }

            PlaceDetailsResult? result = null;
            if (apiResponse.Result is not null && apiResponse.Result.Geometry?.Location is not null)
            {
                result = new PlaceDetailsResult(
                    apiResponse.Result.Name,
                    new PlaceGeometry(
                        new PlaceLocation(
                            apiResponse.Result.Geometry.Location.Lat,
                            apiResponse.Result.Geometry.Location.Lng
                        )
                    )
                );
            }

            return new PlaceDetailsResponse(
                result,
                apiResponse.Status,
                apiResponse.ErrorMessage
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling Google Place Details API");
            return Error.Failure("GoogleMaps.PlaceDetails.HttpError", $"Failed to call Google Maps API: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling Google Place Details API");
            return Error.Unexpected("GoogleMaps.PlaceDetails.UnexpectedError", ex.Message);
        }
    }

    public async Task<ErrorOr<DirectionsResponse>> GetDirectionsAsync(
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude,
        string language = "ar",
        string mode = "driving",
        bool alternatives = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var origin = $"{originLatitude},{originLongitude}";
            var destination = $"{destinationLatitude},{destinationLongitude}";

            var url = $"{_options.BaseUrl}/directions/json" +
                      $"?origin={origin}" +
                      $"&destination={destination}" +
                      $"&language={language}" +
                      $"&mode={mode}" +
                      $"&alternatives={alternatives.ToString().ToLower()}" +
                      $"&key={_options.ApiKey}";

            _logger.LogDebug("Calling Google Directions API from {Origin} to {Destination}", origin, destination);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<GoogleDirectionsApiResponse>(_jsonOptions, cancellationToken);

            if (apiResponse is null)
            {
                return Error.Failure("GoogleMaps.Directions.NullResponse", "Received null response from Google Maps API");
            }

            var routes = apiResponse.Routes?.Select(MapRoute).ToList() ?? new List<DirectionRoute>();

            return new DirectionsResponse(
                routes,
                apiResponse.Status,
                apiResponse.ErrorMessage
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling Google Directions API");
            return Error.Failure("GoogleMaps.Directions.HttpError", $"Failed to call Google Maps API: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling Google Directions API");
            return Error.Unexpected("GoogleMaps.Directions.UnexpectedError", ex.Message);
        }
    }

    private static DirectionRoute MapRoute(GoogleDirectionRoute route)
    {
        return new DirectionRoute(
            new DirectionBounds(
                new LatLng(route.Bounds?.Northeast?.Lat ?? 0, route.Bounds?.Northeast?.Lng ?? 0),
                new LatLng(route.Bounds?.Southwest?.Lat ?? 0, route.Bounds?.Southwest?.Lng ?? 0)
            ),
            route.Copyrights,
            route.Legs?.Select(MapLeg).ToList() ?? new List<DirectionLeg>(),
            new DirectionPolyline(route.OverviewPolyline?.Points ?? string.Empty),
            route.Summary
        );
    }

    private static DirectionLeg MapLeg(GoogleDirectionLeg leg)
    {
        return new DirectionLeg(
            new DirectionValueText(leg.Distance?.Text ?? string.Empty, leg.Distance?.Value ?? 0),
            new DirectionValueText(leg.Duration?.Text ?? string.Empty, leg.Duration?.Value ?? 0),
            leg.EndAddress,
            new LatLng(leg.EndLocation?.Lat ?? 0, leg.EndLocation?.Lng ?? 0),
            leg.StartAddress,
            new LatLng(leg.StartLocation?.Lat ?? 0, leg.StartLocation?.Lng ?? 0),
            leg.Steps?.Select(MapStep).ToList() ?? new List<DirectionStep>()
        );
    }

    private static DirectionStep MapStep(GoogleDirectionStep step)
    {
        return new DirectionStep(
            new DirectionValueText(step.Distance?.Text ?? string.Empty, step.Distance?.Value ?? 0),
            new DirectionValueText(step.Duration?.Text ?? string.Empty, step.Duration?.Value ?? 0),
            new LatLng(step.EndLocation?.Lat ?? 0, step.EndLocation?.Lng ?? 0),
            step.HtmlInstructions,
            new DirectionPolyline(step.Polyline?.Points ?? string.Empty),
            new LatLng(step.StartLocation?.Lat ?? 0, step.StartLocation?.Lng ?? 0),
            step.TravelMode
        );
    }
}
