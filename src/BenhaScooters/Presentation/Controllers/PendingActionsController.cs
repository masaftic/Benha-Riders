using BenhaScooters.Application.Features.PendingActions.Queries;
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
}
