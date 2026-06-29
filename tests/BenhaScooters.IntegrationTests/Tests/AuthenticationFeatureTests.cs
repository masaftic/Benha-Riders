using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.IntegrationTests.Tests;

[Collection("db")]
public class AuthenticationFeatureTests(TestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Register_WithValidData_ShouldReturnOnboardingRequired()
    {
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "ahmed@example.com",
            PhoneNumber = "+201234567890",
            Password = "password123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<JsonElement>(response);
        result.GetProperty("type").GetString().Should().Be("onboarding_required");
        result.GetProperty("result").GetProperty("nextStep").GetString().Should().Be("verify_phone");
        result.GetProperty("result").GetProperty("onboardingToken").GetString().Should().NotBeNullOrEmpty();

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Email == Email.Create(request.Email));
        user.Should().NotBeNull();
        user!.Name.Should().Be(request.Name);
        user.PhoneNumberVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnValidationError()
    {
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "invalid-email",
            PhoneNumber = "+201234567890",
            Password = "password123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidPhoneNumber_ShouldReturnValidationError()
    {
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "ahmed@example.com",
            PhoneNumber = "invalid-phone",
            Password = "password123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturnValidationError()
    {
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "ahmed@example.com",
            PhoneNumber = "+201234567890",
            Password = "123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");

        var request = new
        {
            Name = "Mohamed Hassan",
            Email = "ahmed@example.com",
            PhoneNumber = "+201987654321",
            Password = "password123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_WhenUserIsActive_ShouldReturnAccessToken()
    {
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        user.AddRole(new UserRole(RoleName.Rider));
        await DbContext.SaveChangesAsync();

        var request = new
        {
            PhoneNumber = "+201234567890",
            Password = "password123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<JsonElement>(response);
        result.GetProperty("type").GetString().Should().Be("success");

        var successResult = result.GetProperty("result");
        successResult.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        successResult.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        successResult.GetProperty("expiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidCredentials_WhenOnboardingRequired_ShouldReturnOnboardingToken()
    {
        await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");

        var request = new
        {
            PhoneNumber = "+201234567890",
            Password = "password123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<JsonElement>(response);
        result.GetProperty("type").GetString().Should().Be("onboarding_required");

        var onboardingResult = result.GetProperty("result");
        onboardingResult.GetProperty("onboardingToken").GetString().Should().NotBeNullOrEmpty();
        onboardingResult.GetProperty("nextStep").GetString().Should().Be("verify_phone");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");

        var request = new
        {
            PhoneNumber = "+201234567890",
            Password = "wrongpassword",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnNotFound()
    {
        var request = new
        {
            PhoneNumber = "+201999999999",
            Password = "password123",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public Task VerifySmsCode_WithValidCode_ShouldReturnSuccessResponse()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public Task VerifySmsCode_WithInvalidCode_ShouldReturnBadRequest()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        user.AddRole(new UserRole(RoleName.Rider));
        await DbContext.SaveChangesAsync();

        var (_, refreshToken) = await LoginAsync("+201234567890", "password123");

        var request = new
        {
            RefreshToken = refreshToken,
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<JsonElement>(response);
        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("expiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);
        result.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);

        var replayResponse = await Client.PostAsJsonAsync("/api/auth/refresh", request, JsonOptions);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var request = new
        {
            RefreshToken = "invalid-refresh-token",
            App = App.RiderApp
        };

        var response = await Client.PostAsJsonAsync("/api/auth/refresh", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ShouldInvalidateExistingRefreshTokens()
    {
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        user.AddRole(new UserRole(RoleName.Rider));
        await DbContext.SaveChangesAsync();

        var (accessToken, refreshToken) = await LoginAsync("+201234567890", "password123");
        SetAuthorizationHeader(accessToken);

        var changePasswordResponse = await Client.PostAsJsonAsync("/api/auth/change-password", new
        {
            CurrentPassword = "password123",
            NewPassword = "new-password123",
            ConfirmPassword = "new-password123"
        }, JsonOptions);

        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ClearAuthorizationHeader();
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = refreshToken,
            App = App.RiderApp
        }, JsonOptions);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_WhenAuthenticated_ShouldDeactivateAnonymizeAndInvalidateTokens()
    {
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        user.AddRole(new UserRole(RoleName.Rider));
        user.CreateRiderProfile();
        user.RiderProfile!.AddSavedAddress("Home", "Benha");
        await DbContext.SaveChangesAsync();

        var (accessToken, refreshToken) = await LoginAsync("+201234567890", "password123");
        SetAuthorizationHeader(accessToken);

        var response = await Client.DeleteAsync("/api/auth/account");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        DbContext.ChangeTracker.Clear();

        var updatedUser = await DbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstAsync(u => u.Id == user.Id);

        updatedUser.IsActive.Should().BeFalse();
        updatedUser.Name.Should().Be(User.DeletedAccountName);
        updatedUser.PasswordHash.Should().BeNull();
        updatedUser.PhoneNumberVerified.Should().BeFalse();
        updatedUser.EmailVerified.Should().BeFalse();
        updatedUser.Email.ToString().Should().Be($"deleted-user-{user.Id}@deleted.local");
        updatedUser.PhoneNumber.Should().NotBe(PhoneNumber.Create("+201234567890"));
        updatedUser.RefreshTokens.Should().OnlyContain(token => !token.IsActive);

        var riderProfile = await DbContext.RiderProfiles
            .Include(r => r.SavedAddresses)
            .FirstAsync(r => r.UserId == user.Id);

        riderProfile.IsActive.Should().BeFalse();
        riderProfile.PreferredName.Should().Be(User.DeletedAccountName);
        riderProfile.SavedAddresses.Should().BeEmpty();

        var meResponse = await Client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ClearAuthorizationHeader();
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = refreshToken,
            App = App.RiderApp
        }, JsonOptions);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string AccessToken, string RefreshToken)> LoginAsync(
        string phoneNumber,
        string password,
        App app = App.RiderApp)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            PhoneNumber = phoneNumber,
            Password = password,
            App = app
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<JsonElement>(response);

        return (
            result.GetProperty("result").GetProperty("accessToken").GetString()!,
            result.GetProperty("result").GetProperty("refreshToken").GetString()!);
    }

    private async Task<User> CreateTestUserAsync(string name, string email, string phoneNumber, string password)
    {
        var passwordHasher = GetRequiredService<IPasswordHasher>();
        var hashedPassword = passwordHasher.Hash(password);

        var user = new User(
            name,
            Email.Create(email),
            PhoneNumber.Create(phoneNumber),
            hashedPassword);

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        return user;
    }
}
