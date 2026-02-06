using BenhaScooters.Application.Features.Matching.Commands;
using BenhaScooters.Application.Features.Matching.Queries;
using BenhaScooters.Contracts.Matching;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/matching")]
[Authorize(Policy = "OnboardedDriver")]
public class MatchingController : BaseApiController
{
    private readonly ISender _sender;

    public MatchingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get driver match offers
    /// </summary>
    /// <remarks>
    /// Retrieves pending match offers for the authenticated driver. Returns up to 5 active offers with trip details including pickup/dropoff locations, estimated fare, and expiration times.
    /// </remarks>
    [HttpGet("offers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDriverMatchOffers()
    {
        var query = new GetDriverMatchOffersQuery(HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Accept a match offer
    /// </summary>
    /// <remarks>
    /// Accepts a match offer for a trip. Creates a trip and notifies the rider. Only one match offer can be accepted per trip request. Driver status is automatically set to OnTrip.
    /// </remarks>
    [HttpPost("accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptMatch([FromBody] AcceptMatchRequest request)
    {
        var command = new AcceptMatchCommand(HttpContext.GetDriverId(), request.DriverMatchAttemptId);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Reject a match offer
    /// </summary>
    /// <remarks>
    /// Rejects a match offer. Optionally provide a reason for rejection (e.g., 'too_far', 'low_fare', 'other'). Offer will be marked as rejected and removed from active offers.
    /// </remarks>
    [HttpPost("reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectMatch([FromBody] RejectMatchRequest request)
    {
        var command = new RejectMatchCommand(HttpContext.GetDriverId(), request.DriverMatchAttemptId, request.Reason);
        var result = await _sender.Send(command);

        return result.Match(_ => NoContent(), HandleErrors);
    }
}
