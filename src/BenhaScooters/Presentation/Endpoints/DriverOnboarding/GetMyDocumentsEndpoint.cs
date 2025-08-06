using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class GetMyDocumentsEndpoint : IEndpoint
{
    public record GetMyDocumentsResponse(
        List<string> RequiredDocuments,
        List<string> MissingDocuments,
        List<DocumentDto> Documents);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/drivers/me/onboarding/documents", GetMyDocuments)
            .WithName("GetMyDocuments")
            .WithTags("Driver Onboarding")
            .WithSummary("Get driver's documents")
            .WithDescription("Retrieves the driver's required and submitted documents, including pre-signed URLs for document access.")
            .Produces<GetMyDocumentsResponse>()
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> GetMyDocuments([FromServices] ISender sender, HttpContext ctx)
    {
        var query = new ListDriverDocuments(ctx.GetDriverId());
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var mapper = new GetMyDocumentsEndpointMapper();
        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class GetMyDocumentsEndpointMapper
{
    public partial GetMyDocumentsEndpoint.GetMyDocumentsResponse MapToResponse(ListDriverDocumentsResponse response);
}
