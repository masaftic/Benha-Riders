using BenhaScooters.Domain.Users;

namespace BenhaScooters.Contracts.Authentication;

// ──── Forgot Password ────────────────────────────────────────────────

public record ForgotPasswordRequest(PhoneNumber PhoneNumber);

public record ForgotPasswordResponseDto(string Message, int NextCooldownSeconds);

// ──── Reset Password with OTP ────────────────────────────────────────

public record ResetPasswordWithOtpRequest(
    PhoneNumber PhoneNumber,
    string OtpCode,
    string NewPassword,
    string ConfirmPassword);

public record ResetPasswordResponseDto(string Message);
