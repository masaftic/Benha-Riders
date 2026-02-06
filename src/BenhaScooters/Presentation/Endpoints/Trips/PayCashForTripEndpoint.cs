using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.Trips;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class PayCashForTripEndpoint : IEndpoint
{
    public record PayCashForTripRequest(decimal PaidAmount);
    public record PayCashForTripResponse(string Message, DateTime PaidAt, decimal FinalFare);

    public class PayCashForTripRequestValidator : AbstractValidator<PayCashForTripRequest>
    {
        public PayCashForTripRequestValidator()
        {
            RuleFor(x => x.PaidAmount)
                .GreaterThan(0).WithMessage("Paid amount must be greater than zero.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trips/{tripId}/pay/cash", async ([FromBody] PayCashForTripRequest request, [FromRoute] int tripId, [FromServices] IMediator mediator) =>
        {
            var command = new PayCashForTripCommand(TripId.Create(tripId), request.PaidAmount);
            var result = await mediator.Send(command);

            return result.Match(
                success => Results.Ok(new PayCashForTripResponse(success.Message, success.PaidAt, success.FinalFare)),
                errors => ApiProblem.HandleProblems(errors));
        })
        .WithName("PayCashForTrip")
        .WithTags("Trips - Driver")
        .Produces<PayCashForTripResponse>(StatusCodes.Status200OK)
        .Produces<ValidationProblem>()
        .RequireAuthorization();
    }
}
