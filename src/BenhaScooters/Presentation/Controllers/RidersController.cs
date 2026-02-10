using BenhaScooters.Application.Features.Riders.Queries;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/riders")]
[Authorize(Policy = "RiderPolicy")]
public class RidersController : BaseApiController
{
    private readonly ISender _sender;

    public RidersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get current rider status (idle, requesting, or in_trip)
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus()
    {
        var query = new GetRiderStatusQuery(HttpContext.GetCurrentUserId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }
}
