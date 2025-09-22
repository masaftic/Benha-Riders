using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(string idToken);
}

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(string idToken)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = ["722364989102-to75aj9k5r396b4ounuvjebcjq8b19ld.apps.googleusercontent.com",
                "722364989102-o33m7c5g369r67r65ontfa3q2i595jne.apps.googleusercontent.com"]
        });

        return payload;
    }
}
