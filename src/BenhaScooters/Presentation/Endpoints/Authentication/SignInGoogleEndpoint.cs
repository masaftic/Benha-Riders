
using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Domain;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Authentication;

public class SignInGoogleEndpoint : IEndpoint
{
    public record SigninGoogleRequestDto(string IdToken);

    public record GoogleSignInResponseDto(string Type, object Result);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/signin/google", SignInWithGoogle)
            .WithName("SignInWithGoogle")
            .WithTags("Authentication")
            .WithSummary("Google Sign-In")
            .WithDescription("Authenticates a user with Google ID token. Returns JWT access token and refresh token on successful authentication. If onboarding is required, returns an onboarding token and next step.")
            .Accepts<SigninGoogleRequestDto>("application/json")
            .Produces<GoogleSignInResponseDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .AllowAnonymous()
            .WithOpenApi();
    }

    private async Task<IResult> SignInWithGoogle(SigninGoogleRequestDto request, [FromServices] ISender sender, HttpContext ctx)
    {
        var mapper = new GoogleSignInEndpointMapper();
        var command = mapper.MapToCommand(request);
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
public partial class GoogleSignInEndpointMapper
{
    public partial GoogleSignInCommand MapToCommand(SignInGoogleEndpoint.SigninGoogleRequestDto request);
    
    public SignInGoogleEndpoint.GoogleSignInResponseDto MapToResponse(GoogleSignInResponse response)
    {
        return response switch
        {
            GoogleSignInSuccess success => new SignInGoogleEndpoint.GoogleSignInResponseDto("success", new
            {
                AccessToken = success.AccessToken,
                RefreshToken = success.RefreshToken,
                ExpiresAt = success.ExpiresAt
            }),
            GoogleSignInOnboardingRequired onboarding => new SignInGoogleEndpoint.GoogleSignInResponseDto("onboarding_required", new
            {
                OnboardingToken = onboarding.OnboardingToken,
                NextStep = onboarding.NextStep
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(response))
        };
    }
}
