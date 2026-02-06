using BenhaScooters.Application.Features.Trips.Queries;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class GetDriverRecentTripsEndpoint : IEndpoint
{
    public record GetDriverRecentTripsResponseDto(
        IReadOnlyList<DriverTripSummaryDto> Trips,
        int TotalCount,
        int PageNumber,
        int PageSize,
        bool HasNextPage);

    public record DriverTripSummaryDto(
        TripId TripId,
        string Status,
        string PickupAddress,
        string DropoffAddress,
        decimal EstimatedFare,
        decimal? FinalFare,
        bool IsPaid,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        string RiderName);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trips/recent", GetRecentTrips)
            .WithName("GetDriverRecentTrips")
            .WithTags("Trips - Driver")
            .WithSummary("Get driver's recent trips")
            .WithDescription("Retrieves a paginated list of the driver's recent trips, ordered by creation date (newest first).")
            .Produces<GetDriverRecentTripsResponseDto>()
            .Produces(401)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> GetRecentTrips(
        [FromServices] ISender sender, 
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10, 
        HttpContext ctx = null!)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new GetDriverRecentTripsEndpointMapper();
        var query = mapper.MapToQuery(driverId, pageNumber, pageSize);
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class GetDriverRecentTripsEndpointMapper
{
    public GetDriverRecentTripsQuery MapToQuery(UserId driverId, int pageNumber, int pageSize)
    {
        return new GetDriverRecentTripsQuery(driverId, pageNumber, pageSize);
    }

    [MapperIgnoreSource(nameof(DriverTripSummary.DriverArrivedAt))]
    [MapperIgnoreSource(nameof(DriverTripSummary.StartedAt))]
    [MapProperty(nameof(DriverTripSummary.AssignedAt), nameof(GetDriverRecentTripsEndpoint.DriverTripSummaryDto.CreatedAt))]
    public partial GetDriverRecentTripsEndpoint.DriverTripSummaryDto MapToDto(DriverTripSummary trip);

    public partial GetDriverRecentTripsEndpoint.GetDriverRecentTripsResponseDto MapToResponse(GetDriverRecentTripsResult result);
}
