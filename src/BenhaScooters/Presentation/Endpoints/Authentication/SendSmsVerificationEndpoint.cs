using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class SendSmsVerificationEndpoint : IEndpoint
{
    public record SendSmsVerificationRequestDto(string PhoneNumber);

    public record SendSmsVerificationResponseDto(string Message);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/send-sms-verification", SendSmsVerification)
            .WithName("SendSmsVerification")
            .WithTags("Authentication")
            .WithSummary("Send SMS verification code")
            .WithDescription("Sends a 6-digit verification code to the specified phone number. Code expires after 10 minutes.")
            .Produces<SendSmsVerificationResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(404)
            .RequireAuthorization(p => p.AddRequirements(new UserOnboardingRequirement(OnboardingSteps.VerifyPhone)))
            .WithOpenApi();
    }

    public async Task<IResult> SendSmsVerification([FromServices] ISender sender, [FromBody] SendSmsVerificationRequestDto smsRequest, HttpContext ctx)
    {
        var mapper = new SendSmsVerificationEndpointMapper();
        var command = mapper.MapToCommand(ctx.GetCurrentUserId(), smsRequest);
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
public partial class SendSmsVerificationEndpointMapper
{
    public SendSmsVerificationCommand MapToCommand(UserId userId, SendSmsVerificationEndpoint.SendSmsVerificationRequestDto request) => new(userId, request.PhoneNumber);
    public partial SendSmsVerificationEndpoint.SendSmsVerificationResponseDto MapToResponse(SendSmsVerificationResponse response);
}
