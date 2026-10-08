using BenhaScooters.Application.Features.Authentication.Commands;
using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Application.Features.Authentication.Queries;
using BenhaScooters.Contracts.Authentication;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Controllers;

[Route("api/auth")]
public class AuthenticationController : BaseApiController
{
    private readonly ISender _sender;

    public AuthenticationController(ISender sender)
    {
        _sender = sender;
    }


    /// <summary>
    /// User login
    /// </summary>
    /// <remarks>
    /// Authenticates a user with phone number and password. Returns JWT access token and refresh token on successful authentication. If onboarding is required, returns an onboarding token and next step.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(PhoneNumber.Create(request.PhoneNumber), request.Password, request.App.ToDomain());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <remarks>
    /// Registers a new user in the system.
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.Name,
            Email.Create(request.Email),
            PhoneNumber.Create(request.PhoneNumber),
            request.Password,
            request.App.ToDomain());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <remarks>
    /// Allows users to change their password. Requires current password for verification.
    /// </remarks>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = HttpContext.GetCurrentUserId();
        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmPassword);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Logout user
    /// </summary>
    /// <remarks>
    /// Logs out the current user by invalidating all their refresh tokens. Requires authentication.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = HttpContext.GetCurrentUserId();
        var command = new LogoutCommand(userId, request.RefreshToken);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Deactivate and anonymize the current user's account
    /// </summary>
    /// <remarks>
    /// Preserves trip history and related records, but anonymizes personal information and blocks future access.
    /// </remarks>
    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAccount()
    {
        var command = new DeleteAccountCommand(HttpContext.GetCurrentUserId());
        var result = await _sender.Send(command);

        return result.Match(_ => NoContent(), HandleErrors);
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <remarks>
    /// Returns the current authenticated user's profile information including name, email, phone number, verification status, and roles.
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me()
    {
        var userId = HttpContext.GetCurrentUserId();
        var query = new MeQuery(userId);
        var result = await _sender.Send(query);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Update the current user's preferred language
    /// </summary>
    [HttpPut("preferred-language")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferredLanguage([FromBody] UpdatePreferredLanguageRequest request)
    {
        var command = new SetPreferredLanguageCommand(HttpContext.GetCurrentUserId(), request.Language);
        var result = await _sender.Send(command);

        return result.Match(_ => NoContent(), HandleErrors);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <remarks>
    /// Exchanges a valid refresh token for a new access token and refresh token pair. The old refresh token is invalidated.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken, request.App.ToDomain());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Send SMS verification code
    /// </summary>
    /// <remarks>
    /// Sends a 6-digit verification code to the specified phone number. Code expires after 10 minutes.
    /// </remarks>
    [HttpPost("send-sms-verification")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendSmsVerification([FromBody] SendSmsVerificationRequest request)
    {
        var userId = HttpContext.GetCurrentUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new SendSmsVerificationCommand(userId, PhoneNumber.Create(request.PhoneNumber), ipAddress);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Google Sign-In
    /// </summary>
    /// <remarks>
    /// Authenticates a user with Google ID token. Returns JWT access token and refresh token on successful authentication. If onboarding is required, returns an onboarding token and next step.
    /// </remarks>
    [HttpPost("signin/google")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SignInWithGoogle([FromBody] SignInGoogleRequest request)
    {
        var command = new GoogleSignInCommand(request.IdToken, request.App.ToDomain());
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Verify SMS verification code
    /// </summary>
    /// <remarks>
    /// Verifies the 6-digit SMS code sent to the user's phone number. Marks the phone number as verified upon successful verification.
    /// </remarks>
    [HttpPost("verify-sms-code")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifySmsCode([FromBody] VerifySmsCodeRequest request)
    {
        var userId = HttpContext.GetCurrentUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new VerifySmsCodeCommand(userId, request.Code, request.App.ToDomain(), ipAddress);
        var result = await _sender.Send(command);

        return result.Match(Ok, HandleErrors);
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    /// <remarks>
    /// Sends a 6-digit OTP code to the provided phone number for password reset. Code expires after 5 minutes.
    /// For security reasons, the response does not reveal whether the phone number exists in the system.
    /// </remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new ForgotPasswordCommand(PhoneNumber.Create(request.PhoneNumber), ipAddress);
        var result = await _sender.Send(command);

        return result.Match(
            response => Ok(new ForgotPasswordResponseDto(response.Message, response.NextCooldownSeconds)),
            HandleErrors);
    }

    /// <summary>
    /// Reset password with OTP
    /// </summary>
    /// <remarks>
    /// Verifies the OTP code and resets the password in one step. 
    /// The OTP code must be valid and not expired. All existing sessions will be invalidated after password reset.
    /// </remarks>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new ResetPasswordWithOtpCommand(
            PhoneNumber.Create(request.PhoneNumber),
            request.OtpCode,
            request.NewPassword,
            request.ConfirmPassword,
            ipAddress);
        var result = await _sender.Send(command);

        return result.Match(
            response => Ok(new ResetPasswordResponseDto(response.Message)),
            HandleErrors);
    }

    /// <summary>
    /// Register a device token for push notifications (FCM)
    /// </summary>
    [HttpPost("device-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request)
    {
        var command = new RegisterDeviceTokenCommand(HttpContext.GetCurrentUserId(), request.Token, request.Platform);
        var result = await _sender.Send(command);

        return result.Match(_ => Ok(), HandleErrors);
    }
}
