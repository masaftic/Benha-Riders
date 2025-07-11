using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Features.Authentication;
using BenhaScooters.Domain;
using Microsoft.Extensions.DependencyInjection;
using BenhaScooters.Features.Authentication.Services;

namespace BenhaScooters.IntegrationTests.Authentication;

public class AuthenticationTests : AuthenticationTestBase
{
    public AuthenticationTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = TestDataFactory.CreateRegisterRequest();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var registerResponse = await DeserializeResponse<RegisterResponse>(response);
        Assert.NotNull(registerResponse);
        Assert.True(registerResponse.UserId.Value > 0);
        Assert.True(registerResponse.RequiresPhoneVerification);

        // Verify user was created in database
        var user = await DbContext!.Users.FindAsync(registerResponse.UserId);
        Assert.NotNull(user);
        Assert.Equal(request.Name, user.Name);
        Assert.Equal(request.Email, user.Email.Value);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var request = TestDataFactory.CreateRegisterRequest(email: "invalid-email");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnError()
    {
        // Arrange
        var email = "duplicate@example.com";
        var request1 = TestDataFactory.CreateRegisterRequest(email: email, phoneNumber: "+1234567890");
        var request2 = TestDataFactory.CreateRegisterRequest(email: email, phoneNumber: "+0987654321", role: "driver");

        // Act
        await Client.PostAsJsonAsync("/api/auth/register", request1, JsonOptions);
        var response = await Client.PostAsJsonAsync("/api/auth/register", request2, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var registerRequest = TestDataFactory.CreateRegisterRequest();
        var registerResponse = await RegisterAndVerifyUserAsync(registerRequest);

        var loginRequest = TestDataFactory.CreateLoginRequest(registerRequest.Email, registerRequest.Password);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await DeserializeResponse<LoginResponse>(response);
        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.NotEmpty(loginResponse.RefreshToken);
        Assert.True(loginResponse.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = TestDataFactory.CreateLoginRequest("nonexistent@example.com", "wrongpassword");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ShouldReturnUserInfo()
    {
        // Arrange
        var registerRequest = TestDataFactory.CreateRegisterRequest();
        var (registerResponse, loginResponse) = await RegisterVerifyAndLoginUserAsync(registerRequest);

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meResponse = await DeserializeResponse<MeResponse>(response);
        Assert.NotNull(meResponse);
        Assert.Equal(registerRequest.Name, meResponse.Name);
        Assert.Equal(registerRequest.Email, meResponse.Email.Value);
        Assert.False(meResponse.EmailVerified);
        Assert.Contains(RoleName.Rider, meResponse.Roles);

        // Clean up
        ClearAuthorizationHeader();
    }

    [Fact]
    public async Task Register_WithDriverRole_ShouldCreateDriverUser()
    {
        // Arrange
        var request = TestDataFactory.Driver.CreateRegisterRequest();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var registerResponse = await DeserializeResponse<RegisterResponse>(response);
        Assert.NotNull(registerResponse);

        // Verify user has driver role
        var user = await DbContext!.Users
            .Include(u => u.Roles)
            .FirstAsync(u => u.Id == registerResponse.UserId);

        Assert.Contains(user.Roles, r => r.Name == RoleName.Driver);
    }
}
