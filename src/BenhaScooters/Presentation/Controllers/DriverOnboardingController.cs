using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Contracts.DriverOnboarding;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/drivers/me/onboarding")]
[Authorize(Roles = "Driver")]
public class DriverOnboardingController : BaseApiController
{
    private readonly ISender _sender;

    public DriverOnboardingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get driver onboarding status
    /// </summary>
    /// <remarks>
    /// Retrieves the current onboarding status of the driver including progress and any rejection reasons. Creates a new driver profile if none exists.
    /// </remarks>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOnboardingStatus()
    {
        var query = new GetOnboardingStatusQuery(HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get driver onboarding details
    /// </summary>
    /// <remarks>
    /// Retrieves comprehensive onboarding details including personal information, vehicle information, documents, and current status for the authenticated driver.
    /// </remarks>
    [HttpGet("details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOnboardingDetails()
    {
        var query = new GetOnboardingDetailsQuery(HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Get driver's documents
    /// </summary>
    /// <remarks>
    /// Retrieves the driver's required and submitted documents, including pre-signed URLs for document access.
    /// </remarks>
    [HttpGet("documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyDocuments()
    {
        var query = new ListDriverDocuments(HttpContext.GetDriverId());
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Update driver personal information
    /// </summary>
    /// <remarks>
    /// Updates the driver's personal information including name, national ID, date of birth, address, and emergency contact details during the onboarding process.
    /// </remarks>
    [HttpPost("personal-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoRequest request)
    {
        var driverId = HttpContext.GetDriverId();
        var command = new UpdatePersonalInfoCommand(
            driverId,
            request.FullName,
            request.NationalId);

        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Update driver vehicle information
    /// </summary>
    /// <remarks>
    /// Updates the driver's vehicle information including make, model, year, license plate, and color during the onboarding process. Vehicle year must be between 1980 and current year + 1.
    /// </remarks>
    [HttpPost("vehicle-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVehicleInfo([FromBody] UpdateVehicleInfoRequest request)
    {
        var driverId = HttpContext.GetDriverId();
        var command = new UpdateVehicleInfoCommand(
            driverId,
            request.VehicleType,
            request.VehicleBrand,
            request.VehicleModel,
            request.VehicleColor,
            request.LicensePlate,
            request.VehicleYear);

        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Update driver documents
    /// </summary>
    /// <remarks>
    /// Uploads driver documents including license image, vehicle registration image, and driver photo. Files are stored securely in S3 storage. Accepted formats: JPG, JPEG, PNG (max 10MB each).
    /// </remarks>
    [HttpPost("documents")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDocuments([FromForm] UpdateDocumentsRequest request)
    {
        var driverId = HttpContext.GetDriverId();
        
        // Build dictionary of document types to files
        var documents = new Dictionary<DocumentType, IFormFile>();
        if (request.LicenseImage != null)
            documents[DocumentType.DrivingLicense] = request.LicenseImage;
        if (request.VehicleRegistrationImage != null)
            documents[DocumentType.VehicleRegistration] = request.VehicleRegistrationImage;
        if (request.DriverImage != null)
            documents[DocumentType.DriverPhoto] = request.DriverImage;
        
        var command = new UpdateDocumentsCommand(driverId, documents);

        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Upload a document for driver onboarding
    /// </summary>
    /// <remarks>
    /// Allows drivers to upload required documents for onboarding. If image existed, it will be replaced if it isn't approved already. Types of documents are: DrivingLicense, VehicleRegistration, DriverPhoto
    /// </remarks>
    [HttpPost("documents/upload")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentRequest request)
    {
        var command = new UploadDocumentCommand(
            HttpContext.GetDriverId(),
            Enum.Parse<DocumentType>(request.DocumentType),
            request.File);

        var result = await _sender.Send(command);

        return result.Match(_ => NoContent(), HandleErrors);
    }

    /// <summary>
    /// Submit driver application for review
    /// </summary>
    /// <remarks>
    /// Sends driver application to admins to review, driver can freely edit his onboarding info before submission or if rejected
    /// </remarks>
    [HttpPost("submit-for-review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitForReview()
    {
        var command = new SubmitForReviewCommand(HttpContext.GetDriverId());
        var result = await _sender.Send(command);

        return result.Match(_ => Ok(), HandleErrors);
    }
}
