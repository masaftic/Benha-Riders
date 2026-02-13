using BenhaScooters.Application.Abstractions;
using BenhaScooters.Contracts.GoogleMaps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

/// <summary>
/// Proxy endpoints for Google Maps API to keep API key secure on the server
/// </summary>
[Route("api/maps")]
[Authorize]
public class GoogleMapsController : BaseApiController
{
    private readonly IGoogleMapsService _googleMapsService;

    public GoogleMapsController(IGoogleMapsService googleMapsService)
    {
        _googleMapsService = googleMapsService;
    }

    /// <summary>
    /// Reverse geocode coordinates to get formatted address
    /// </summary>
    /// <param name="request">The coordinates and language preference</param>
    /// <returns>The formatted address for the given coordinates</returns>
    [HttpGet("geocode/reverse")]
    [ProducesResponseType(typeof(ReverseGeocodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReverseGeocode([FromQuery] ReverseGeocodeRequest request)
    {
        var result = await _googleMapsService.ReverseGeocodeAsync(
            request.Latitude,
            request.Longitude,
            request.Language
        );

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get place autocomplete suggestions
    /// </summary>
    /// <param name="request">The search input and optional filters</param>
    /// <returns>List of place predictions matching the search query</returns>
    [HttpGet("places/autocomplete")]
    [ProducesResponseType(typeof(AutocompleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Autocomplete([FromQuery] AutocompleteRequest request)
    {
        var result = await _googleMapsService.AutocompleteAsync(
            request.Input,
            request.Language,
            request.Latitude,
            request.Longitude,
            request.Components,
            request.Radius,
            request.Types
        );

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get details for a specific place
    /// </summary>
    /// <param name="request">The place ID and optional parameters</param>
    /// <returns>Detailed information about the place including coordinates</returns>
    [HttpGet("places/details")]
    [ProducesResponseType(typeof(PlaceDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPlaceDetails([FromQuery] PlaceDetailsRequest request)
    {
        var result = await _googleMapsService.GetPlaceDetailsAsync(
            request.PlaceId,
            request.Language,
            request.Fields
        );

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get directions between two points
    /// </summary>
    /// <param name="request">Origin and destination coordinates with optional parameters</param>
    /// <returns>Route information including distance, duration, and polyline</returns>
    [HttpGet("directions")]
    [ProducesResponseType(typeof(DirectionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDirections([FromQuery] DirectionsRequest request)
    {
        var result = await _googleMapsService.GetDirectionsAsync(
            request.OriginLatitude,
            request.OriginLongitude,
            request.DestinationLatitude,
            request.DestinationLongitude,
            request.Language,
            request.Mode,
            request.Alternatives
        );

        return result.Match(Ok, HandleErrors);
    }
}
