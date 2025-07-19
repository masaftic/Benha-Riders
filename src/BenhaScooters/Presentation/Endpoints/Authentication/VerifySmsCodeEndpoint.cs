using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Security;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class VerifySmsCodeEndpoint : IEndpoint
{
    public record VerifySmsCodeRequestDto(string Code);

    public record VerifySmsCodeResponseDto(string OnboardingToken, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/verify-sms-code", VerifySmsCode)
            .WithName("VerifySmsCode")
            .WithTags("Authentication")
            .WithSummary("Verify SMS verification code")
            .WithDescription("Verifies the 6-digit SMS code sent to the user's phone number. Marks the phone number as verified upon successful verification.")
            .Produces<VerifySmsCodeResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(404)
            .RequireAuthorization(p => p.AddRequirements(new UserOnboardingRequirement(OnboardingSteps.VerifyPhone)))
            .WithOpenApi();
    }

    public async Task<IResult> VerifySmsCode([FromServices] ISender sender, [FromBody] VerifySmsCodeRequestDto verifyRequest, HttpContext ctx)
    {
        var userId = ctx.GetCurrentUserId();
        var mapper = new VerifySmsCodeEndpointMapper();
        var command = mapper.MapToCommand(userId, verifyRequest);
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
public partial class VerifySmsCodeEndpointMapper
{
    public VerifySmsCodeCommand MapToCommand(UserId userId, VerifySmsCodeEndpoint.VerifySmsCodeRequestDto request) => 
        new VerifySmsCodeCommand(userId, request.Code);
    public partial VerifySmsCodeEndpoint.VerifySmsCodeResponseDto MapToResponse(OnboardingStatusToken response);
}
