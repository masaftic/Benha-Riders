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
            Audience = new[] { _configuration["GoogleAuth:ClientId"] }
        });

        return payload;
    }
}
