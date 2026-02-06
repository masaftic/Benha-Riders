using BenhaScooters.Application.Features.Drivers.Commands;
using BenhaScooters.Application.Features.Drivers.Queries;
using BenhaScooters.Contracts.Drivers;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/driver")]
[Authorize(Policy = "OnboardedDriver")]
public class DriversController : BaseApiController
{
    private readonly ISender _sender;

    public DriversController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get current driver availability status
    /// </summary>
    /// <remarks>
    /// Retrieves the driver's current availability status, location information, session duration, and current trip details if applicable. Only accessible by completed onboarded drivers.
    /// </remarks>
    [HttpGet("availability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDriverAvailability()
    {
        var query = new GetDriverAvailabilityQuery(HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Set driver availability status
    /// </summary>
    /// <remarks>
    /// Updates the driver's availability status (Online, Offline, Busy). Drivers must be online to receive trip requests. OnTrip status is set automatically and cannot be manually assigned. Only accessible by completed onboarded drivers.
    /// </remarks>
    [HttpPost("availability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDriverAvailability([FromBody] SetDriverAvailabilityRequest request)
    {
        var command = new SetDriverAvailabilityCommand(HttpContext.GetDriverId(), request.Status.ToString());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Update current driver location
    /// </summary>
    /// <remarks>
    /// Updates the driver's current GPS location, heading, and speed. Used for real-time tracking and trip monitoring. Latitude must be between -90 and 90, longitude between -180 and 180, heading between 0 and 360 degrees, and speed must be non-negative. Only accessible by completed onboarded drivers.
    /// </remarks>
    [HttpPut("location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var command = new UpdateLocationCommand(HttpContext.GetDriverId(), request.Latitude, request.Longitude);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }
}
