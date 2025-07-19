using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Security;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class SelectRoleEndpoint : IEndpoint
{
    public record SelectRoleRequestDto(string Role);

    public record SelectRoleResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/select-role", SelectRole)
            .WithName("SelectRole")
            .WithTags("Authentication")
            .WithSummary("Select user role after SMS verification")
            .WithDescription("Assigns a role (Rider or Driver) to a user after phone verification.")
            .Produces<SelectRoleResponseDto>()
            .ProducesValidationProblem()
            .RequireAuthorization(p => p.AddRequirements(new UserOnboardingRequirement(OnboardingSteps.SelectRole)))
            .WithOpenApi();
    }

    public async Task<IResult> SelectRole([FromServices] ISender sender, [FromBody] SelectRoleRequestDto request, HttpContext ctx)
    {
        var userId = ctx.GetCurrentUserId();
        var mapper = new SelectRoleEndpointMapper();
        var command = mapper.MapToCommand(userId, request);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class SelectRoleEndpointMapper
{
    public SelectRoleCommand MapToCommand(UserId userId, SelectRoleEndpoint.SelectRoleRequestDto request) =>
        new SelectRoleCommand(userId, request.Role);
    public partial SelectRoleEndpoint.SelectRoleResponseDto MapToResponse(AuthenticatedResponse response);

    private static UserId IntToUserId(int userId) => UserId.From(userId);
}
