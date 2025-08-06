using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class GetDriverDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/drivers/{driverId}", GetDriverDetails)
            .WithName("GetDriverDetails")
            .WithTags("Admin - Driver Management")
            .WithSummary("Get driver details")
            .WithDescription("Retrieves the details of a specific driver.")
            .Produces(StatusCodes.Status200OK)
            .Produces<GetOnboardingDetailsResponse>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> GetDriverDetails([FromServices] ISender sender, [FromRoute] int driverId, HttpContext ctx)
    {
        var query = new GetOnboardingDetailsQuery(DriverId.From(driverId));
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.Ok(result.Value);
    }
}
