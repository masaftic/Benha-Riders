using BenhaScooters.Application.Features.TripRequests.Commands;
using BenhaScooters.Application.Features.TripRequests.Queries;
using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Contracts.TripRequests;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Infrastructure.Security;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/trip-requests")]
[Authorize(Policy = PolicyConstants.RiderPolicy)]
public class TripRequestsController : BaseApiController
{
    private readonly ISender _sender;

    public TripRequestsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Request a new trip
    /// </summary>
    /// <remarks>
    /// Creates a new trip request with pickup and dropoff locations. Calculates estimated fare and distance using the Haversine formula.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestTrip([FromBody] RequestTripRequest request)
    {
        var pickupCoordinate = request.PickupLatitude.ToCoordinate(request.PickupLongitude);
        
        var dropoffCoordinate = request.DropoffLatitude.ToCoordinate(request.DropoffLongitude);

        var command = new RequestTripCommand(
            HttpContext.GetCurrentUserId(),
            pickupCoordinate,
            dropoffCoordinate,
            request.DropoffAddress);

        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Cancel a trip request
    /// </summary>
    /// <remarks>
    /// Allows riders to cancel their pending trip requests. Only pending trips can be cancelled.
    /// </remarks>
    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTripRequest([FromBody] CancelTripRequestRequest request)
    {
        var command = new CancelTripRequestCommand(request.TripRequestId.ToTripRequestId(), HttpContext.GetCurrentUserId(), request.CancellationReason);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Confirm a trip request (after driver accepted)
    /// </summary>
    [HttpPost("{tripRequestId}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmTripRequest([FromRoute] TripRequestId tripRequestId)
    {
        var command = new ConfirmTripRequestCommand(tripRequestId, HttpContext.GetCurrentUserId());
        var result = await _sender.Send(command);

        return result.Match(_ => Ok(), HandleErrors);
    }

    /// <summary>
    /// Get current active trip request for the rider
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentTripRequest()
    {
        var query = new GetCurrentTripRequestQuery(HttpContext.GetCurrentUserId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get trip request by ID
    /// </summary>
    [HttpGet("{tripRequestId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTripRequestById([FromRoute] TripRequestId tripRequestId)
    {
        var query = new GetTripRequestByIdQuery(tripRequestId, HttpContext.GetCurrentUserId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get trip status
    /// </summary>
    [HttpGet("{tripRequestId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTripStatus([FromRoute] TripRequestId tripRequestId)
    {
        var query = new GetTripStatusQuery(tripRequestId, HttpContext.GetCurrentUserId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    // /// <summary>
    // /// Get available trips (for admins to see all pending trip requests)
    // /// </summary>
    // [HttpGet("available")]
    // [Authorize(Roles = "Admin")]
    // [ProducesResponseType(StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    // [ProducesResponseType(StatusCodes.Status403Forbidden)]
    // public async Task<IActionResult> GetAvailableTrips()
    // {
    //     var query = new GetAvailableTripsQuery(HttpContext.GetCurrentUserId());
    //     var result = await _sender.Send(query);

    //     return result.Match(Ok, HandleErrors);
    // }
}
