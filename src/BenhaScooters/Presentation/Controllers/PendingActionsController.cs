using BenhaScooters.Application.Features.PendingActions.Commands;
using BenhaScooters.Application.Features.PendingActions.Queries;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Infrastructure.Security;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/pending-actions")]
[Authorize(Policy = PolicyConstants.PhoneVerifiedPolicy)]
public class PendingActionsController(ISender sender) : BaseApiController
{
    /// <summary>
    /// Get pending actions for the authenticated user.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<GetPendingActionsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingActions()
    {
        var query = new GetPendingActionsQuery(HttpContext.GetCurrentUserId());
        var result = await sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Dismiss a pending driver rating action for a completed trip.
    /// </summary>
    [HttpPost("trip-driver-rating/{tripId}/dismiss")]
    [Authorize(Policy = PolicyConstants.RiderPolicy)]
    [ProducesResponseType<DismissDriverRatingActionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissDriverRatingAction([FromRoute] TripId tripId)
    {
        var command = new DismissDriverRatingActionCommand(tripId, HttpContext.GetRiderId());
        var result = await sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }
}
