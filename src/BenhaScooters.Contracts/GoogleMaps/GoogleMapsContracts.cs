using System.Text.Json.Serialization;

namespace BenhaScooters.Contracts.GoogleMaps;

// ==================== Geocode ====================
public record ReverseGeocodeRequest(
    double Latitude,
    double Longitude,
    string Language = "ar"
);

public record ReverseGeocodeResponse(
    string? FormattedAddress,
    string Status,
    string? ErrorMessage
);

// ==================== Autocomplete ====================
public record AutocompleteRequest(
    string Input,
    string Language = "ar",
    double? Latitude = null,
    double? Longitude = null,
    string? Components = "country:eg",
    int Radius = 50000,
    string Types = "geocode|establishment"
);

public record AutocompleteResponse(
    List<AutocompletePrediction> Predictions,
    string Status,
    string? ErrorMessage
);

public record AutocompletePrediction(
    string Description,
    string PlaceId,
    string Reference,
    StructuredFormatting? StructuredFormatting,
    List<string>? Types
);

public record StructuredFormatting(
    string MainText,
    string? SecondaryText
);

// ==================== Place Details ====================
public record PlaceDetailsRequest(
    string PlaceId,
    string Language = "ar",
    string Fields = "name,geometry"
);

public record PlaceDetailsResponse(
    PlaceDetailsResult? Result,
    string Status,
    string? ErrorMessage
);

public record PlaceDetailsResult(
    string Name,
    PlaceGeometry Geometry
);

public record PlaceGeometry(
    PlaceLocation Location
);

public record PlaceLocation(
    double Lat,
    double Lng
);

// ==================== Directions ====================
public record DirectionsRequest(
    double OriginLatitude,
    double OriginLongitude,
    double DestinationLatitude,
    double DestinationLongitude,
    string Language = "ar",
    string Mode = "driving",
    bool Alternatives = false
);

public record DirectionsResponse(
    List<DirectionRoute> Routes,
    string Status,
    string? ErrorMessage
);

public record DirectionRoute(
    DirectionBounds Bounds,
    string Copyrights,
    List<DirectionLeg> Legs,
    DirectionPolyline OverviewPolyline,
    string Summary
);

public record DirectionBounds(
    LatLng Northeast,
    LatLng Southwest
);

public record LatLng(
    double Lat,
    double Lng
);

public record DirectionLeg(
    DirectionValueText Distance,
    DirectionValueText Duration,
    string EndAddress,
    LatLng EndLocation,
    string StartAddress,
    LatLng StartLocation,
    List<DirectionStep> Steps
);

public record DirectionValueText(
    string Text,
    int Value
);

public record DirectionStep(
    DirectionValueText Distance,
    DirectionValueText Duration,
    LatLng EndLocation,
    string HtmlInstructions,
    DirectionPolyline Polyline,
    LatLng StartLocation,
    string TravelMode
);

public record DirectionPolyline(
    string Points
);

