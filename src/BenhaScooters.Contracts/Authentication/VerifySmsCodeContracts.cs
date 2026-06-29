using BenhaScooters.Domain.Users;

namespace BenhaScooters.Contracts.Authentication;

public record VerifySmsCodeRequest(string Code, App App);
