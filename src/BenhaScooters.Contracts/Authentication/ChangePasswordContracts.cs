namespace BenhaScooters.Contracts.Authentication;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
