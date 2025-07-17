using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Endpoints.DriverOnboarding.Common;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class GetPendingApplicationsEndpoint : IEndpoint
{
    public record GetPendingApplicationsResponseDto(List<PendingDriverApplicationDto> Applications);

    public record PendingDriverApplicationDto(
        int UserId,
        PersonalInfoDto? PersonalInfo,
        VehicleInfoDto? VehicleInfo,
        DocumentsDto? Documents,
        DateTime CreatedAt);


    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/driver/pending-applications", GetPendingApplications)
            .WithName("GetPendingDriverApplications")
            .WithTags("Admin - Driver Management")
            .WithSummary("Get pending driver applications for review")
            .WithDescription("Retrieves all driver applications that are pending review by administrators. Includes complete application details with pre-signed document URLs valid for 15 minutes.")
            .Produces<GetPendingApplicationsResponseDto>()
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> GetPendingApplications([FromServices] ISender sender, HttpContext ctx)
    {
        var query = new GetPendingApplicationsQuery();
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var mapper = new GetPendingApplicationsEndpointMapper();
        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class GetPendingApplicationsEndpointMapper
{
    public partial GetPendingApplicationsEndpoint.GetPendingApplicationsResponseDto MapToResponse(GetPendingApplicationsResponse response);
}
