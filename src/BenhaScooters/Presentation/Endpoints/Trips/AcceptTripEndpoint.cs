// using BenhaScooters.Application.Features.Trips.Commands;
// using BenhaScooters.Domain.Trips;
// using BenhaScooters.Presentation.Endpoints;
// using MediatR;
// using Microsoft.AspNetCore.Http.HttpResults;
// using Microsoft.AspNetCore.Mvc;
// using Riok.Mapperly.Abstractions;

// namespace BenhaScooters.Presentation.Endpoints.Trips;

// public class AcceptTripEndpoint : IEndpoint
// {
//     public record AcceptTripRequestDto(int TripRequestId);

//     public record AcceptTripResponseDto(
//         int TripId,
//         string Message,
//         DateTime AcceptedAt);

//     public void MapEndpoint(IEndpointRouteBuilder app)
//     {
//         app.MapPost("/trips/accept", AcceptTrip)
//             .WithName("AcceptTrip")
//             .WithTags("Trips - Driver")
//             .WithSummary("Accept a trip request")
//             .WithDescription("Allows drivers to accept a pending trip request. Creates a Trip entity and updates driver availability to OnTrip status.")
//             .Produces<AcceptTripResponseDto>()
//             .ProducesValidationProblem()
//             .Produces(401)
//             .Produces(404)
//             .RequireAuthorization("DriverPolicy")
//             .WithOpenApi();
//     }

//     public async Task<IResult> AcceptTrip([FromServices] ISender sender, [FromBody] AcceptTripRequestDto request, HttpContext ctx)
//     {
//         var driverId = ctx.GetDriverId();
//         var mapper = new AcceptTripEndpointMapper();
//         var command = mapper.MapToCommand(request, driverId);
//         var result = await sender.Send(command);

//         if (result.IsError)
//         {
//             return ApiProblem.HandleProblems(result.Errors, ctx);
//         }

//         var response = mapper.MapToResponse(result.Value);
//         return Results.Ok(response);
//     }
// }

// [Mapper]
// public partial class AcceptTripEndpointMapper
// {
//     public AcceptTripCommand MapToCommand(AcceptTripEndpoint.AcceptTripRequestDto request, Domain.Drivers.DriverId driverId)
//     {
//         return new AcceptTripCommand(
//             driverId,
//             TripRequestId.From(request.TripRequestId));
//     }

//     public AcceptTripEndpoint.AcceptTripResponseDto MapToResponse(AcceptTripResult result)
//     {
//         return new AcceptTripEndpoint.AcceptTripResponseDto(
//             result.TripId.Value,
//             result.Message,
//             result.AcceptedAt);
//     }
// }
