using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Application.Features.Trips.Queries;
using BenhaScooters.Contracts.Trips;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Infrastructure.Security;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/trips")]
[Authorize(Policy = PolicyConstants.PhoneVerifiedPolicy)]
public class TripsController : BaseApiController
{
    private readonly ISender _sender;

    public TripsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get current active trip for the authenticated user (works for both riders and drivers)
    /// </summary>
    [HttpGet("current")]
    [Authorize]
    [ProducesResponseType<GetCurrentTripResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentTrip()
    {
        var query = new GetCurrentTripQuery(HttpContext.GetCurrentUserId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get paginated trip history for the authenticated user (works for both riders and drivers)
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    [ProducesResponseType<GetTripHistoryResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTripHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = new GetTripHistoryQuery(HttpContext.GetCurrentUserId(), page, pageSize);
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get driver's recent trips
    /// </summary>
    [HttpGet("recent")]
    [Authorize(Policy = PolicyConstants.ApprovedDriverPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDriverRecentTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = new GetDriverRecentTripsQuery(HttpContext.GetDriverId(), page, pageSize);
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get driver trip by ID
    /// </summary>
    [HttpGet("{tripId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTripById([FromRoute] TripId tripId)
    {
        var query = new GetDriverTripByIdQuery(tripId, HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Mark driver as arrived at pickup location
    /// </summary>
    [HttpPost("{tripId}/arrived")]
    [Authorize(Policy = PolicyConstants.ApprovedDriverPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DriverArrived([FromRoute] TripId tripId)
    {
        var command = new DriverArrivedCommand(tripId, HttpContext.GetDriverId());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Start the trip
    /// </summary>
    [HttpPost("{tripId}/start")]
    [Authorize(Policy = PolicyConstants.ApprovedDriverPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartTrip([FromRoute] TripId tripId)
    {
        var command = new StartTripCommand(tripId, HttpContext.GetDriverId());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Complete the trip
    /// </summary>
    [HttpPost("{tripId}/complete")]
    [Authorize(Policy = PolicyConstants.ApprovedDriverPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteTrip([FromRoute] TripId tripId)
    {
        var command = new CompleteTripCommand(tripId, HttpContext.GetDriverId());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    [HttpPost("{tripId}/cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTrip([FromRoute] TripId tripId, [FromBody] CancelTripRequest request)
    {
        var command = new CancelTripCommand(tripId, HttpContext.GetCurrentUserId(), request.CancellationReason);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Rate the driver after a completed trip (rider only)
    /// </summary>
    [HttpPost("{tripId}/rate")]
    [Authorize(Policy = PolicyConstants.RiderPolicy)]
    [ProducesResponseType<RateDriverResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RateDriver([FromRoute] TripId tripId, [FromBody] RateDriverRequest request)
    {
        var command = new RateDriverCommand(tripId, HttpContext.GetRiderId(), request.Rating, request.Comment);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }
}
