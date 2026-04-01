using System.Text.Json;

namespace BenhaScooters.Data.MapData;


/* "type": "Feature", 
      "properties": {
        "source": "https://simplemaps.com", 
        "id": "EGKB", 
        "name": "Al Qalyubiyah"
      }, 
      "id": 22 */


internal record MapFeatureCollection(
    string Type, // FeatureCollection
    List<MapAreaPolygon> Features);

internal record MapAreaPolygon(
    int Id, 
    string Type, // Feature
    Properties Properties, 
    Geometry Geometry);

internal record Geometry(
    string Type, // Polygon or MultiPolygon
    JsonElement Coordinates);

internal record Properties(string Source, string Id, string Name);