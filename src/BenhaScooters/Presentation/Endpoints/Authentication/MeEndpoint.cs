using BenhaScooters.Application.Features.Authentication.Queries;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class MeEndpoint : IEndpoint
{
    public record MeResponseDto(
        UserId Id,
        string Name,
        Email Email,
        bool EmailVerified,
        PhoneNumber PhoneNumber,
        bool PhoneNumberVerified,
        DateTime CreatedAt,
        IEnumerable<string> Roles);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithTags("Authentication")
            .WithSummary("Get current user information")
            .WithDescription("Returns the current authenticated user's profile information including name, email, phone number, verification status, and roles.")
            .Produces<MeResponseDto>()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization()
            .WithOpenApi();
    }

    public async Task<IResult> GetCurrentUser([FromServices] ISender sender, HttpContext ctx)
    {
        var userId = ctx.GetCurrentUserId();
        var query = new MeQuery(userId);
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var mapper = new MeEndpointMapper();
        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class MeEndpointMapper
{
    public partial MeEndpoint.MeResponseDto MapToResponse(MeResponse response);

    private static IEnumerable<string> RoleNamesToStrings(IEnumerable<RoleName> roleNames) => roleNames.Select(r => r.ToString());
}
