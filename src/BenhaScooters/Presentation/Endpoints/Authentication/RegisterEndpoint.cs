using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Domain;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class RegisterEndpoint : IEndpoint
{
    public record RegisterRequestDto(
        string Name,
        string Email,
        string PhoneNumber,
        string Password,
        string Role);

    public record RegisterResponseDto(string Message, int UserId, bool RequiresPhoneVerification);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", RegisterUser)
            .WithName("RegisterUser")
            .WithTags("Authentication")
            .WithSummary("Register a new user")
            .WithDescription("Registers a new user in the system.")
            .Produces<RegisterResponseDto>()
            .ProducesValidationProblem()
            .AllowAnonymous()
            .WithOpenApi();
    }

    public async Task<IResult> RegisterUser([FromServices] ISender sender, [FromBody] RegisterRequestDto registerRequest, HttpContext ctx)
    {
        var mapper = new RegisterEndpointMapper();
        var command = mapper.MapToCommand(registerRequest);
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
public partial class RegisterEndpointMapper
{
    public partial RegisterCommand MapToCommand(RegisterEndpoint.RegisterRequestDto request);
    public partial RegisterEndpoint.RegisterResponseDto MapToResponse(RegisterResponse response);

    private static int UserIdToInt(UserId userId) => userId.Value;
}