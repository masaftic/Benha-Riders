using BenhaScooters.Application.Features.Matching.Queries;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Matching;

public class GetDriverMatchOffersEndpoint : IEndpoint
{
    public record GetDriverMatchOffersResponseDto(
        List<DriverMatchOfferResponseDto> MatchOffers);

    public record DriverMatchOfferResponseDto(
        DriverMatchAttemptId DriverMatchAttemptId,
        double PickupLatitude,
        double PickupLongitude,
        double DropoffLatitude,
        double DropoffLongitude,
        string? PickupAddress,
        string? DropoffAddress,
        decimal EstimatedFare,
        DateTime OfferedAt,
        DateTime ExpiresAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/matching/offers", GetDriverMatchOffers)
            .WithName("GetDriverMatchOffers")
            .WithTags("Matching - Driver")
            .WithSummary("Get pending match offers for driver")
            .WithDescription("Allows drivers to see their pending trip match offers. This is a fallback endpoint until push notifications are implemented.")
            .Produces<GetDriverMatchOffersResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> GetDriverMatchOffers([FromServices] ISender sender, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new GetDriverMatchOffersEndpointMapper();
        var query = mapper.MapToQuery(driverId);
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
public partial class GetDriverMatchOffersEndpointMapper
{
    public GetDriverMatchOffersQuery MapToQuery(UserId driverId) =>
        new(driverId);

    public partial GetDriverMatchOffersEndpoint.GetDriverMatchOffersResponseDto MapToResponse(GetDriverMatchOffersResult result);
}
