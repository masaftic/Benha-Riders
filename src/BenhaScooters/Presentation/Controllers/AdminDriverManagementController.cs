using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Contracts.DriverOnboarding;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
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
        DriverOnboardingStatus? onboardingStatus = string.IsNullOrEmpty(queryParams.OnboardingStatus)
            ? null
            : Enum.Parse<DriverOnboardingStatus>(queryParams.OnboardingStatus);

        var query = new ListDriversWithFilters(
            onboardingStatus,
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
    /// Get driver documents
    /// </summary>
    /// <remarks>
    /// Retrieves the documents submitted by a specific driver for administrative review.
    /// </remarks>
    [HttpGet("{driverId}/documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDriverDocuments([FromRoute] int driverId)
    {
        var query = new ListDriverDocuments(UserId.Create(driverId));
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
}
