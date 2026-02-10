using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace BenhaScooters.IntegrationTests;

/// <summary>
/// Integration tests for authentication endpoints
/// </summary>
public class AuthenticationFeatureTests : FeatureTestBase
{
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationFeatureTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        _passwordHasher = Factory.Services.GetRequiredService<IPasswordHasher>();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOnboardingToken()
    {
        // Arrange
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "ahmed@example.com",
            PhoneNumber = "+201234567890",
            Password = "password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("onboardingToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("nextStep").GetString().Should().Be("verify_phone");

        // Verify user was created in database
        var user = await DbContext!.Users.FirstOrDefaultAsync(u => u.Email == Email.Create(request.Email));
        user.Should().NotBeNull();
        user!.Name.Should().Be(request.Name);
        user.Status.Should().Be(UserStatus.Registered);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "invalid-email",
            PhoneNumber = "+201234567890",
            Password = "password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidPhoneNumber_ShouldReturnValidationError()
    {
        // Arrange
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "ahmed@example.com",
            PhoneNumber = "invalid-phone",
            Password = "password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturnValidationError()
    {
        // Arrange
        var request = new
        {
            Name = "Ahmed Ali",
            Email = "ahmed@example.com",
            PhoneNumber = "+201234567890",
            Password = "123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");

        var request = new
        {
            Name = "Mohamed Hassan",
            Email = "ahmed@example.com", // Same email
            PhoneNumber = "+201987654321",
            Password = "password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_WhenUserIsActive_ShouldReturnAccessToken()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();

        user.AddRole(new(RoleName.Rider));
        await DbContext!.SaveChangesAsync();

        var request = new
        {
            PhoneNumber = "+201234567890",
            Password = "password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("type").GetString().Should().Be("success");
        var successResult = result.GetProperty("result");
        successResult.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        successResult.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        successResult.GetProperty("expiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidCredentials_WhenOnboardingRequired_ShouldReturnOnboardingToken()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        // User is in Registered status, so onboarding is required

        var request = new
        {
            PhoneNumber = "+201234567890",
            Password = "password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("type").GetString().Should().Be("onboarding_required");
        var onboardingResult = result.GetProperty("result");
        onboardingResult.GetProperty("onboardingToken").GetString().Should().NotBeNullOrEmpty();
        onboardingResult.GetProperty("nextStep").GetString().Should().Be("verify_phone");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");

        var request = new
        {
            PhoneNumber = "+201234567890",
            Password = "wrongpassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var request = new
        {
            PhoneNumber = "+201999999999",
            Password = "password123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Send SMS Verification Tests

    [Fact]
    public async Task SendSmsVerification_WithValidOnboardingToken_ShouldReturnSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        var onboardingToken = GenerateOnboardingTokenAsync(user.Id, user.Status, UserOnboardingStateMachine.GetNextStep(user.Status));

        SetAuthorizationHeader(onboardingToken);

        var request = new
        {
            PhoneNumber = "+201234567890"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/send-sms-verification", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendSmsVerification_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            PhoneNumber = "+201234567890"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/send-sms-verification", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Verify SMS Code Tests

    [Fact]
    public async Task VerifySmsCode_WithValidCode_ShouldReturnOnboardingToken()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        var onboardingToken = GenerateOnboardingTokenAsync(user.Id, user.Status, UserOnboardingStateMachine.GetNextStep(user.Status));

        // Set up SMS verification code
        var code = SmsVerificationCode.GenerateCode();
        var verificationCode = new SmsVerificationCode(user.Id, user.PhoneNumber!, code, TimeSpan.FromMinutes(10));

        DbContext!.SmsVerificationCodes.Add(verificationCode);
        await DbContext!.SaveChangesAsync();

        SetAuthorizationHeader(onboardingToken);

        var request = new
        {
            Code = code
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/verify-sms-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("onboardingToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("nextStep").GetString().Should().Be("select_role");

        // Verify user status was updated
        await DbContext.Entry(user).ReloadAsync();
        user.Status.Should().Be(UserStatus.PhoneVerified);
    }

    [Fact]
    public async Task VerifySmsCode_WithInvalidCode_ShouldReturnBadRequest()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        var onboardingToken = GenerateOnboardingTokenAsync(user.Id, user.Status, UserOnboardingStateMachine.GetNextStep(user.Status));

        var code = SmsVerificationCode.GenerateCode();
        var verificationCode = new SmsVerificationCode(user.Id, user.PhoneNumber!, code, TimeSpan.FromMinutes(10));

        DbContext!.Add(verificationCode);
        await DbContext!.SaveChangesAsync();

        SetAuthorizationHeader(onboardingToken);

        var request = new
        {
            Code = "000000" // Invalid code
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/verify-sms-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Select Role Tests

    [Fact]
    public async Task SelectRole_WithValidRole_ShouldReturnAccessToken()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        await DbContext!.SaveChangesAsync();

        var onboardingToken = GenerateOnboardingTokenAsync(user.Id, user.Status, UserOnboardingStateMachine.GetNextStep(user.Status));
        SetAuthorizationHeader(onboardingToken);

        var request = new
        {
            Role = "Rider"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/select-role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("expiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);

        // Verify user status and role were updated
        await DbContext.Entry(user).ReloadAsync();
        user.Status.Should().Be(UserStatus.Active);
        await DbContext.Entry(user).Collection(u => u.Roles).LoadAsync();
        user.Roles.Should().NotBeEmpty();
        user.Roles.Select(x => x.Name).Should().Contain(RoleName.Rider);
    }

    [Fact]
    public async Task SelectRole_WithDriverRole_ShouldReturnAccessToken()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        await DbContext!.SaveChangesAsync();

        var onboardingToken = GenerateOnboardingTokenAsync(user.Id, user.Status, UserOnboardingStateMachine.GetNextStep(user.Status));
        SetAuthorizationHeader(onboardingToken);

        var request = new
        {
            Role = "Driver"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/select-role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();

        // Verify user role was updated
        await DbContext!.Entry(user).ReloadAsync();
        await DbContext.Entry(user).Collection(u => u.Roles).LoadAsync();
        user.Roles.Should().NotBeEmpty();
        user.Roles.Select(x => x.Name).Should().Contain(RoleName.Driver);
    }

    [Fact]
    public async Task SelectRole_WithInvalidRole_ShouldReturnBadRequest()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        await DbContext!.SaveChangesAsync();

        var onboardingToken = GenerateOnboardingTokenAsync(user.Id, user.Status, UserOnboardingStateMachine.GetNextStep(user.Status));
        SetAuthorizationHeader(onboardingToken);

        var request = new
        {
            Role = "InvalidRole"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/select-role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync("Ahmed Ali", "ahmed@example.com", "+201234567890", "password123");
        user.VerifyPhoneNumber();
        user.AddRole(new UserRole(RoleName.Rider));
        await DbContext!.SaveChangesAsync();

        // Login to get refresh token
        var loginRequest = new
        {
            PhoneNumber = "+201234567890",
            Password = "password123"
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent, JsonOptions);
        var refreshToken = loginResult.GetProperty("result").GetProperty("refreshToken").GetString();

        var request = new
        {
            RefreshToken = refreshToken
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("expiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);

        // New refresh token should be different from the old one
        result.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string name, string email, string phoneNumber, string password)
    {
        var hashedPassword = _passwordHasher.Hash(password);

        var user = new User(
            name,
            Email.Create(email),
            PhoneNumber.Create(phoneNumber),
            hashedPassword
        );

        DbContext!.Users.Add(user);
        await DbContext.SaveChangesAsync();

        return user;
    }

    private string GenerateOnboardingTokenAsync(UserId userId, UserStatus userStatus, string nextStep)
    {
        var jwtService = Factory.Services.GetRequiredService<IJwtService>();
        return jwtService.GenerateOnboardingToken(userId, userStatus, nextStep);
    }

    #endregion
}
