using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Application.Features.Trips.Queries;
using BenhaScooters.Contracts.Trips;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/trips")]
[Authorize(Policy = "OnboardedDriver")]
public class TripsController : BaseApiController
{
    private readonly ISender _sender;

    public TripsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get driver's current trip
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDriverCurrentTrip()
    {
        var query = new GetDriverCurrentTripQuery(HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get driver's recent trips
    /// </summary>
    [HttpGet("recent")]
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
    public async Task<IActionResult> GetDriverTripById([FromRoute] TripId tripId)
    {
        var query = new GetDriverTripByIdQuery(tripId, HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Mark driver as arrived at pickup location
    /// </summary>
    [HttpPost("{tripId}/arrived")]
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

    /// <summary>
    /// Pay cash for trip
    /// </summary>
    [HttpPost("{tripId}/pay/cash")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayCashForTrip([FromRoute] int tripId, [FromBody] PayCashForTripRequest request)
    {
        var command = new PayCashForTripCommand(TripId.Create(tripId), request.PaidAmount);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }
}
