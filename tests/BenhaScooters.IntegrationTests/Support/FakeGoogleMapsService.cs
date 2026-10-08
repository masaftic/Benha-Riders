using BenhaScooters.Application.Abstractions;
using BenhaScooters.Contracts.GoogleMaps;
using ErrorOr;

namespace BenhaScooters.IntegrationTests.Support;

public sealed class FakeGoogleMapsService : IGoogleMapsService
{
    public Task<ErrorOr<ReverseGeocodeResponse>> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        string language = "ar",
        CancellationToken cancellationToken = default)
    {
        var response = new ReverseGeocodeResponse(
            $"Test address ({latitude:F5}, {longitude:F5})",
            "OK",
            null);

        return Task.FromResult<ErrorOr<ReverseGeocodeResponse>>(response);
    }

    public Task<ErrorOr<AutocompleteResponse>> AutocompleteAsync(
        string input,
        string language = "ar",
        double? latitude = null,
        double? longitude = null,
        string? components = "country:eg",
        int radius = 50000,
        string types = "geocode|establishment",
        CancellationToken cancellationToken = default)
    {
        var response = new AutocompleteResponse([], "OK", null);
        return Task.FromResult<ErrorOr<AutocompleteResponse>>(response);
    }

    public Task<ErrorOr<PlaceDetailsResponse>> GetPlaceDetailsAsync(
        string placeId,
        string language = "ar",
        string fields = "name,geometry",
        CancellationToken cancellationToken = default)
    {
        var response = new PlaceDetailsResponse(
            new PlaceDetailsResult(
                "Test place",
                new PlaceGeometry(new PlaceLocation(TestGeoFixtures.BenhaStation.Lat, TestGeoFixtures.BenhaStation.Lng))),
            "OK",
            null);

        return Task.FromResult<ErrorOr<PlaceDetailsResponse>>(response);
    }

    public Task<ErrorOr<DirectionsResponse>> GetDirectionsAsync(
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude,
        string language = "ar",
        string mode = "driving",
        bool alternatives = false,
        CancellationToken cancellationToken = default)
    {
        var distanceMeters = TestGeoFixtures.HaversineMeters(
            originLatitude,
            originLongitude,
            destinationLatitude,
            destinationLongitude);

        var durationSeconds = Math.Max(60, (int)Math.Round(distanceMeters / 1000d / 30d * 60d * 60d));

        var response = new DirectionsResponse(
            [
                new DirectionRoute(
                    new DirectionBounds(
                        new LatLng(Math.Max(originLatitude, destinationLatitude), Math.Max(originLongitude, destinationLongitude)),
                        new LatLng(Math.Min(originLatitude, destinationLatitude), Math.Min(originLongitude, destinationLongitude))),
                    "Test",
                    [
                        new DirectionLeg(
                            new DirectionValueText($"{distanceMeters / 1000d:F1} km", (int)Math.Round(distanceMeters)),
                            new DirectionValueText($"{durationSeconds / 60d:F0} mins", durationSeconds),
                            "Test destination",
                            new LatLng(destinationLatitude, destinationLongitude),
                            "Test origin",
                            new LatLng(originLatitude, originLongitude),
                            [])
                    ],
                    new DirectionPolyline(""),
                    "Test route")
            ],
            "OK",
            null);

        return Task.FromResult<ErrorOr<DirectionsResponse>>(response);
    }
}
