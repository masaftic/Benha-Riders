using BenhaScooters.Application.Features.DriverOnboarding.Queries;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class GetDriverRejectedFieldsEndpoint : IEndpoint
{
    public record RejectionReasonResponseDto(string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/drivers/me/onboarding/rejection-reason", GetRejectionReason)
            .WithName("GetDriverRejectionReason")
            .WithTags("Driver Onboarding")
            .WithSummary("Get driver rejection reason")
            .WithDescription("Retrieves the rejection reason for a driver's profile when the driver's application status is 'Rejected'.")
            .Produces<RejectionReasonResponseDto>()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> GetRejectionReason([FromServices] ISender sender, HttpContext ctx)
    {
        var query = new GetDriverRejectionReasonQuery(ctx.GetDriverId());
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var response = new RejectionReasonResponseDto(result.Value.Reason);
        return Results.Ok(response);
    }
}
