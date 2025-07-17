using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class LogoutEndpoint : IEndpoint
{
    public record LogoutRequestDto(string? RefreshToken = null);

    public record LogoutResponseDto(string Message);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", LogoutUser)
            .WithName("LogoutUser")
            .WithTags("Authentication")
            .WithSummary("Logout user")
            .WithDescription("Logs out the current user by invalidating all their refresh tokens. Requires authentication.")
            .Produces<LogoutResponseDto>()
            .Produces(401)
            .RequireAuthorization()
            .WithOpenApi();
    }

    public async Task<IResult> LogoutUser([FromServices] ISender sender, [FromBody] LogoutRequestDto logoutRequest, HttpContext ctx)
    {
        var userId = ctx.GetCurrentUserId();
        var mapper = new LogoutEndpointMapper();
        var command = mapper.MapToCommand(logoutRequest, userId);
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
public partial class LogoutEndpointMapper
{
    public LogoutCommand MapToCommand(LogoutEndpoint.LogoutRequestDto request, UserId userId)
    {
        return new LogoutCommand(userId, request.RefreshToken);
    }

    public partial LogoutEndpoint.LogoutResponseDto MapToResponse(LogoutResponse response);
}
