using BenhaScooters.Features.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace BenhaScooters.IntegrationTests.Authentication;

/// <summary>
/// Base class for authentication feature integration tests
/// </summary>
public abstract class AuthenticationTestBase : FeatureTestBase
{
    protected AuthenticationTestBase(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    /// <summary>
    /// Registers a user and returns the registration response
    /// </summary>
    protected async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<RegisterResponse>(response) ?? 
               throw new InvalidOperationException("Failed to deserialize register response");
    }

    /// <summary>
    /// Registers a user and verifies their phone number, returning the user
    /// </summary>
    protected async Task<RegisterResponse> RegisterAndVerifyUserAsync(RegisterRequest request)
    {
        var registerResponse = await RegisterUserAsync(request);
        
        // Skip SMS verification by directly updating the database
        var user = await DbContext!.Users.FindAsync(registerResponse.UserId);
        if (user != null)
        {
            user.VerifyPhoneNumber();
            await DbContext.SaveChangesAsync();
        }

        return registerResponse;
    }

    /// <summary>
    /// Logs in a user and returns the login response
    /// </summary>
    protected async Task<LoginResponse> LoginUserAsync(LoginRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<LoginResponse>(response) ?? 
               throw new InvalidOperationException("Failed to deserialize login response");
    }

    /// <summary>
    /// Registers, verifies, and logs in a user, then sets the authorization header
    /// </summary>
    protected async Task<(RegisterResponse registerResponse, LoginResponse loginResponse)> RegisterVerifyAndLoginUserAsync(RegisterRequest registerRequest)
    {
        var registerResponse = await RegisterAndVerifyUserAsync(registerRequest);
        
        var loginRequest = TestDataFactory.CreateLoginRequest(registerRequest.Email, registerRequest.Password);
        var loginResponse = await LoginUserAsync(loginRequest);
        
        SetAuthorizationHeader(loginResponse.AccessToken);
        
        return (registerResponse, loginResponse);
    }

    /// <summary>
    /// Sends SMS verification code for a phone number
    /// </summary>
    protected async Task<HttpResponseMessage> SendSmsVerificationAsync(string phoneNumber)
    {
        var request = TestDataFactory.CreateSmsVerificationRequest(phoneNumber);
        return await Client.PostAsJsonAsync("/api/auth/send-sms-verification", request, JsonOptions);
    }

    /// <summary>
    /// Verifies SMS code for a phone number
    /// </summary>
    protected async Task<HttpResponseMessage> VerifySmsCodeAsync(string phoneNumber, string code)
    {
        var request = TestDataFactory.CreateVerifySmsCodeRequest(phoneNumber, code);
        return await Client.PostAsJsonAsync("/api/auth/verify-sms-code", request, JsonOptions);
    }

    /// <summary>
    /// Gets the current user info using the /me endpoint
    /// </summary>
    protected async Task<MeResponse?> GetCurrentUserAsync()
    {
        var response = await Client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<MeResponse>(response);
    }

    /// <summary>
    /// Gets the latest SMS verification code for a user from the database
    /// </summary>
    protected async Task<string?> GetLatestSmsCodeForUserAsync(string phoneNumber)
    {
        var user = await DbContext!.Users.FirstOrDefaultAsync(u => u.PhoneNumber.Value == phoneNumber);
        if (user == null) return null;

        var smsCode = await DbContext.SmsVerificationCodes
            .Where(s => s.UserId == user.Id && !s.IsUsed)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        return smsCode?.Code;
    }

    /// <summary>
    /// Creates and returns a sample rider registration request using the test data factory
    /// </summary>
    protected RegisterRequest CreateRiderRequest() => TestDataFactory.Rider.CreateRegisterRequest();

    /// <summary>
    /// Creates and returns a sample driver registration request using the test data factory
    /// </summary>
    protected RegisterRequest CreateDriverRequest() => TestDataFactory.Driver.CreateRegisterRequest();

    /// <summary>
    /// Refreshes an entity from the database to see changes made by API endpoints
    /// </summary>
    protected async Task RefreshEntityAsync<T>(T entity) where T : class
    {
        await DbContext!.Entry(entity).ReloadAsync();
    }

    /// <summary>
    /// Gets a fresh instance of an entity from the database by its ID
    /// </summary>
    protected async Task<T?> GetFreshEntityAsync<T>(object id) where T : class
    {
        return await DbContext!.Set<T>().FindAsync(id);
    }
}
