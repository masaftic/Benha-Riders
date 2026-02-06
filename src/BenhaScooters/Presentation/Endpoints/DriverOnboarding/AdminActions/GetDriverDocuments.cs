
using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class GetDriverDocuments : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/drivers/{driverId}/documents", GetDocuments)
            .WithName("GetDriverDocuments")
            .WithTags("Admin - Driver Management")
            .WithSummary("Get driver documents")
            .WithDescription("Retrieves the documents submitted by a specific driver for administrative review.")
            .Produces<ListDriverDocumentsResponse>()
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> GetDocuments([FromServices] ISender sender, [FromRoute] int driverId, HttpContext ctx)
    {
        var query = new ListDriverDocuments(UserId.Create(driverId));
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.Ok(result.Value);
    }
}
