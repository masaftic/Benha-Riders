using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class ChangePasswordEndpoint : IEndpoint
{
    public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword, string ConfirmPassword);

    public record ChangePasswordResponseDto(string Message);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/change-password", ChangePassword)
            .WithName("ChangePassword")
            .WithTags("Authentication")
            .WithSummary("Change user password")
            .WithDescription("Allows users to change their password. Requires current password for verification.")
            .Produces<ChangePasswordResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .RequireAuthorization()
            .WithOpenApi();
    }

    public async Task<IResult> ChangePassword([FromServices] ISender sender, [FromBody] ChangePasswordRequestDto changePasswordRequest, HttpContext ctx)
    {
        var userId = ctx.GetCurrentUserId();
        var mapper = new ChangePasswordEndpointMapper();
        var command = mapper.MapToCommand(changePasswordRequest, userId);
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
public partial class ChangePasswordEndpointMapper
{
    public ChangePasswordCommand MapToCommand(ChangePasswordEndpoint.ChangePasswordRequestDto request, UserId userId)
    {
        return new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmPassword);
    }

    public partial ChangePasswordEndpoint.ChangePasswordResponseDto MapToResponse(ChangePasswordResponse response);
}
