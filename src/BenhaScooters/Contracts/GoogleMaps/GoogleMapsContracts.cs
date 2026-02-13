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

// ==================== Internal Google API Response Models ====================
// These are used internally to deserialize Google's API responses

internal class GoogleGeocodeApiResponse
{
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
    
    [JsonPropertyName("results")]
    public List<GoogleGeocodeResult>? Results { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

internal class GoogleGeocodeResult
{
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; } = string.Empty;
}

internal class GoogleAutocompleteApiResponse
{
    [JsonPropertyName("predictions")]
    public List<GooglePrediction>? Predictions { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal class GooglePrediction
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = string.Empty;
    
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
    
    [JsonPropertyName("structured_formatting")]
    public GoogleStructuredFormatting? StructuredFormatting { get; set; }
    
    [JsonPropertyName("types")]
    public List<string>? Types { get; set; }
}

internal class GoogleStructuredFormatting
{
    [JsonPropertyName("main_text")]
    public string MainText { get; set; } = string.Empty;
    
    [JsonPropertyName("secondary_text")]
    public string? SecondaryText { get; set; }
}

internal class GooglePlaceDetailsApiResponse
{
    [JsonPropertyName("result")]
    public GooglePlaceDetailsResult? Result { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal class GooglePlaceDetailsResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("geometry")]
    public GoogleGeometry? Geometry { get; set; }
}

internal class GoogleGeometry
{
    [JsonPropertyName("location")]
    public GoogleLocation? Location { get; set; }
}

internal class GoogleLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }
    
    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

internal class GoogleDirectionsApiResponse
{
    [JsonPropertyName("routes")]
    public List<GoogleDirectionRoute>? Routes { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal class GoogleDirectionRoute
{
    [JsonPropertyName("bounds")]
    public GoogleDirectionBounds? Bounds { get; set; }
    
    [JsonPropertyName("copyrights")]
    public string Copyrights { get; set; } = string.Empty;
    
    [JsonPropertyName("legs")]
    public List<GoogleDirectionLeg>? Legs { get; set; }
    
    [JsonPropertyName("overview_polyline")]
    public GoogleDirectionPolyline? OverviewPolyline { get; set; }
    
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

internal class GoogleDirectionBounds
{
    [JsonPropertyName("northeast")]
    public GoogleLocation? Northeast { get; set; }
    
    [JsonPropertyName("southwest")]
    public GoogleLocation? Southwest { get; set; }
}

internal class GoogleDirectionLeg
{
    [JsonPropertyName("distance")]
    public GoogleValueText? Distance { get; set; }
    
    [JsonPropertyName("duration")]
    public GoogleValueText? Duration { get; set; }
    
    [JsonPropertyName("end_address")]
    public string EndAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("end_location")]
    public GoogleLocation? EndLocation { get; set; }
    
    [JsonPropertyName("start_address")]
    public string StartAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("start_location")]
    public GoogleLocation? StartLocation { get; set; }
    
    [JsonPropertyName("steps")]
    public List<GoogleDirectionStep>? Steps { get; set; }
}

internal class GoogleValueText
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

internal class GoogleDirectionStep
{
    [JsonPropertyName("distance")]
    public GoogleValueText? Distance { get; set; }
    
    [JsonPropertyName("duration")]
    public GoogleValueText? Duration { get; set; }
    
    [JsonPropertyName("end_location")]
    public GoogleLocation? EndLocation { get; set; }
    
    [JsonPropertyName("html_instructions")]
    public string HtmlInstructions { get; set; } = string.Empty;
    
    [JsonPropertyName("polyline")]
    public GoogleDirectionPolyline? Polyline { get; set; }
    
    [JsonPropertyName("start_location")]
    public GoogleLocation? StartLocation { get; set; }
    
    [JsonPropertyName("travel_mode")]
    public string TravelMode { get; set; } = string.Empty;
}

internal class GoogleDirectionPolyline
{
    [JsonPropertyName("points")]
    public string Points { get; set; } = string.Empty;
}
