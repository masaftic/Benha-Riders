using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class RefreshTokenEndpoint : IEndpoint
{
    public record RefreshTokenRequestDto(string RefreshToken);

    public record RefreshTokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", RefreshAccessToken)
            .WithName("RefreshAccessToken")
            .WithTags("Authentication")
            .WithSummary("Refresh access token")
            .WithDescription("Exchanges a valid refresh token for a new access token and refresh token pair. The old refresh token is invalidated.")
            .Produces<RefreshTokenResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .AllowAnonymous()
            .WithOpenApi();
    }

    public async Task<IResult> RefreshAccessToken([FromServices] ISender sender, [FromBody] RefreshTokenRequestDto refreshRequest, HttpContext ctx)
    {
        var mapper = new RefreshTokenEndpointMapper();
        var command = mapper.MapToCommand(refreshRequest);
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
public partial class RefreshTokenEndpointMapper
{
    public partial RefreshTokenCommand MapToCommand(RefreshTokenEndpoint.RefreshTokenRequestDto request);
    public partial RefreshTokenEndpoint.RefreshTokenResponseDto MapToResponse(RefreshTokenResponse response);
}
