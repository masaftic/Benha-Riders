using BenhaScooters.Application.Features.Drivers.Queries;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Drivers;

public class GetDriverAvailabilityEndpoint : IEndpoint
{
    public record GetDriverAvailabilityResponseDto(
        string Status,
        DateTime LastStatusChange,
        TimeSpan? OnlineSessionDuration,
        TimeSpan TotalOnlineTime,
        int? CurrentTripId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/driver/availability", GetDriverAvailability)
            .WithName("GetDriverAvailability")
            .WithTags("Driver Management")
            .WithSummary("Get current driver availability status")
            .WithDescription("Retrieves the driver's current availability status, location information, session duration, and current trip details if applicable. Only accessible by completed onboarded drivers.")
            .Produces<GetDriverAvailabilityResponseDto>()
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("OnboardedDriver")
            .WithOpenApi();
    }

    public async Task<IResult> GetDriverAvailability([FromServices] ISender sender, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        
        var query = new GetDriverAvailabilityQuery(driverId);
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var mapper = new GetDriverAvailabilityEndpointMapper();
        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class GetDriverAvailabilityEndpointMapper
{
    public partial GetDriverAvailabilityEndpoint.GetDriverAvailabilityResponseDto MapToResponse(GetDriverAvailabilityResponse response);

    private static string DriverStatusToString(DriverStatus status) => status.ToString();
}
