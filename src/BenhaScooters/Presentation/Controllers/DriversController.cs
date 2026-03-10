using BenhaScooters.Application.Features.Drivers.Commands;
using BenhaScooters.Application.Features.Drivers.Queries;
using BenhaScooters.Contracts.Drivers;
using BenhaScooters.Contracts.Wallet;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Security;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/driver")]
[Authorize(Policy = PolicyConstants.ApprovedDriverPolicy)]
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

    // ── Wallet ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Get wallet summary
    /// </summary>
    /// <remarks>
    /// Returns the driver's current wallet balance, debt, debt limit status, total commissions charged, total amount paid in, and pending top-up request count.
    /// </remarks>
    [HttpGet("wallet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWalletSummary()
    {
        var query = new GetWalletSummaryQuery(HttpContext.GetDriverId());
        var result = await _sender.Send(query);
        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get wallet transactions (paginated)
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of wallet transactions (commissions and top-ups) filtered by date range and/or transaction type.
    /// </remarks>
    [HttpGet("wallet/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWalletTransactions([FromQuery] GetWalletTransactionsParams p)
    {
        var query = new GetWalletTransactionsQuery(
            HttpContext.GetDriverId(),
            p.Page,
            p.PageSize,
            p.From,
            p.To,
            p.Type);
        var result = await _sender.Send(query);
        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Submit a top-up (cash payment) request
    /// </summary>
    /// <remarks>
    /// The driver uploads a receipt image and the claimed amount. An admin will manually approve or reject the request. Only one pending request is allowed at a time.
    /// </remarks>
    [HttpPost("wallet/topup")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitTopUpRequest([FromForm] SubmitTopUpRequestRequest request, IFormFile receipt)
    {
        var command = new SubmitTopUpRequestCommand(HttpContext.GetDriverId(), request.Amount, receipt);
        var result = await _sender.Send(command);
        return result.Match(r => CreatedAtAction(nameof(GetMyTopUpRequests), r), HandleErrors);
    }

    /// <summary>
    /// List driver's own top-up requests
    /// </summary>
    /// <remarks>
    /// Returns the driver's top-up request history (all statuses), ordered by newest first.
    /// </remarks>
    [HttpGet("wallet/topup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyTopUpRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetTopUpRequestsQuery(page, pageSize, null, HttpContext.GetDriverId());
        var result = await _sender.Send(query);
        return result.Match(Ok, HandleErrors);
    }
}
