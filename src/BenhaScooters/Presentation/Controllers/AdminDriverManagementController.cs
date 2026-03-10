using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Application.Features.Drivers.Commands;
using BenhaScooters.Application.Features.Drivers.Queries;
using BenhaScooters.Contracts.DriverOnboarding;
using BenhaScooters.Contracts.Wallet;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/admin/drivers")]
[Authorize(Roles = "Admin")]
public class AdminDriverManagementController : BaseApiController
{
    private readonly ISender _sender;

    public AdminDriverManagementController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get filtered driver applications for review
    /// </summary>
    /// <remarks>
    /// Retrieves driver applications that match the specified filters. If OnboardingStatus is not provided, returns drivers with any status. Includes complete application details with pre-signed document URLs valid for 15 minutes.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFilteredDrivers([FromQuery] QueryDriversParams queryParams)
    {
        var query = new ListDriversWithFilters(
            queryParams.OnboardingStatus,
            queryParams.Page,
            queryParams.PageCount);

        var result = await _sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get driver details
    /// </summary>
    /// <remarks>
    /// Retrieves the details of a specific driver.
    /// </remarks>
    [HttpGet("{driverId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDriverDetails([FromRoute] int driverId)
    {
        var query = new GetOnboardingDetailsQuery(UserId.Create(driverId));
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Approve driver onboarding application
    /// </summary>
    /// <remarks>
    /// Approves a driver's onboarding application, changing their status to 'Completed' and allowing them to start accepting rides. This action is irreversible.
    /// </remarks>
    [HttpPost("{driverId}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveDriver([FromRoute] int driverId)
    {
        var command = new ApproveDriverCommand(UserId.Create(driverId));
        var result = await _sender.Send(command);

        return result.Match(_ => NoContent(), HandleErrors);
    }

    /// <summary>
    /// Ban driver onboarding application
    /// </summary>
    /// <remarks>
    /// Ban a driver's onboarding application with a specific reason.
    /// </remarks>
    [HttpPost("{driverId}/ban")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanDriver([FromRoute] int driverId, [FromBody] BanDriverRequest request)
    {
        var command = new BanDriverCommand(UserId.Create(driverId), request.Reason);
        var result = await _sender.Send(command);

        return result.Match(_ => NoContent(), HandleErrors);
    }

    // ── Wallet top-up review ──────────────────────────────────────────────────

    /// <summary>
    /// List top-up requests
    /// </summary>
    /// <remarks>
    /// Returns all driver top-up (cash payment) requests. Filter by status (Pending/Approved/Rejected) or by a specific driver.
    /// </remarks>
    [HttpGet("wallet/topup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopUpRequests([FromQuery] GetTopUpRequestsParams p)
    {
        UserId? driverFilter = p.DriverId.HasValue ? UserId.Create(p.DriverId.Value) : null;
        var query = new GetTopUpRequestsQuery(p.Page, p.PageSize, p.Status, driverFilter);
        var result = await _sender.Send(query);
        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Approve or reject a top-up request
    /// </summary>
    /// <remarks>
    /// Approve: credits the driver's wallet with the requested amount and changes status to Approved. Reject: marks the request as Rejected (must provide a reason). Either action is final.
    /// </remarks>
    [HttpPost("wallet/topup/{requestId}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReviewTopUpRequest([FromRoute] int requestId, [FromBody] ReviewTopUpRequestRequest request)
    {
        var adminId = HttpContext.GetCurrentUserId();
        var command = new ReviewTopUpRequestCommand(
            WalletTopUpRequestId.Create(requestId),
            adminId,
            request.Approve,
            request.Note);
        var result = await _sender.Send(command);
        return result.Match(_ => NoContent(), HandleErrors);
    }
}
