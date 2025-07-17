using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class LoginEndpoint : IEndpoint
{
    public record LoginRequestDto(string Email, string Password);

    public record LoginResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", LoginUser)
            .WithName("LoginUser")
            .WithTags("Authentication")
            .WithSummary("User login")
            .WithDescription("Authenticates a user with email and password. Returns JWT access token and refresh token on successful authentication.")
            .Produces<LoginResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(403)
            .AllowAnonymous()
            .WithOpenApi();
    }

    public async Task<IResult> LoginUser([FromServices] ISender sender, [FromBody] LoginRequestDto loginRequest, HttpContext ctx)
    {
        var mapper = new LoginEndpointMapper();
        var command = mapper.MapToCommand(loginRequest);
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
public partial class LoginEndpointMapper
{
    public partial LoginCommand MapToCommand(LoginEndpoint.LoginRequestDto request);
    public partial LoginEndpoint.LoginResponseDto MapToResponse(LoginResponse response);
}
