using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Features.Authentication;
using BenhaScooters.Domain;

namespace BenhaScooters.IntegrationTests.Authentication;

public class SmsVerificationTests : AuthenticationTestBase
{
    public SmsVerificationTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }


    [Fact]
    public async Task SendSmsVerification_WithValidPhoneNumber_ShouldSendCode()
    {
        // Arrange
        var registerRequest = TestDataFactory.CreateRegisterRequest();
        var registerResponse = await RegisterUserAsync(registerRequest);

        var smsRequest = TestDataFactory.CreateSmsVerificationRequest(registerRequest.PhoneNumber);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/send-sms-verification", smsRequest, JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify SMS code was created in database
        var user = await DbContext!.Users.FirstAsync(u => u.PhoneNumber == PhoneNumber.From(registerRequest.PhoneNumber));
        var smsCode = await DbContext.SmsVerificationCodes
            .FirstOrDefaultAsync(s => s.UserId == user.Id);
        
        Assert.NotNull(smsCode);
        Assert.False(smsCode.IsUsed);
        Assert.True(smsCode.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task SendSmsVerification_WithNonExistentPhoneNumber_ShouldReturnNotFound()
    {
        // Arrange
        var smsRequest = TestDataFactory.CreateSmsVerificationRequest("+209999999999");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/send-sms-verification", smsRequest, JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VerifySmsCode_WithValidCode_ShouldVerifyPhone()
    {
        // Arrange
        var registerRequest = TestDataFactory.CreateRegisterRequest();
        await RegisterUserAsync(registerRequest);

        var smsRequest = TestDataFactory.CreateSmsVerificationRequest(registerRequest.PhoneNumber);
        await Client.PostAsJsonAsync("/api/auth/send-sms-verification", smsRequest, JsonOptions);

        // Get the code from database (in real scenario this would be sent via SMS)
        var user = await DbContext!.Users.FirstAsync(u => u.PhoneNumber == PhoneNumber.From(registerRequest.PhoneNumber));
        var smsCode = await DbContext.SmsVerificationCodes
            .FirstAsync(s => s.UserId == user.Id && !s.IsUsed);

        var verifyRequest = TestDataFactory.CreateVerifySmsCodeRequest(registerRequest.PhoneNumber, smsCode.Code);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/verify-sms-code", verifyRequest, JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var verifyResponse = await DeserializeResponse<VerifySmsCodeResponse>(response);
        Assert.NotNull(verifyResponse);
        Assert.Equal("Phone number verified successfully.", verifyResponse.Message);
        Assert.True(verifyResponse.IsVerified);

        // Verify user's phone is now verified
        var updatedUser = await DbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
            
        Assert.NotNull(updatedUser);
        Assert.True(updatedUser.PhoneNumberVerified);

        // Verify SMS code is marked as used
        var updatedSmsCode = await DbContext.SmsVerificationCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == smsCode.Id);

        Assert.NotNull(updatedSmsCode);
        Assert.True(updatedSmsCode.IsUsed);
    }

    [Fact]
    public async Task VerifySmsCode_WithInvalidCode_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = TestDataFactory.CreateRegisterRequest();
        await RegisterUserAsync(registerRequest);

        var verifyRequest = TestDataFactory.CreateVerifySmsCodeRequest(registerRequest.PhoneNumber, "999999");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/verify-sms-code", verifyRequest, JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifySmsCode_WithExpiredCode_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = TestDataFactory.CreateRegisterRequest();
        await RegisterUserAsync(registerRequest);

        var user = await DbContext!.Users.FirstAsync(u => u.PhoneNumber == PhoneNumber.From(registerRequest.PhoneNumber));

        // Create an expired SMS code manually by first creating a valid one and then updating it
        var expiredCode = new SmsVerificationCode(
            user.Id,
            user.PhoneNumber,
            "123456",
            TimeSpan.FromMinutes(5) // Create as valid first
        );
        
        DbContext.SmsVerificationCodes.Add(expiredCode);
        await DbContext.SaveChangesAsync();
        
        // Update the expiration time to make it expired
        DbContext.Entry(expiredCode).Property(e => e.ExpiresAt).CurrentValue = DateTime.UtcNow.AddMinutes(-10);
        await DbContext.SaveChangesAsync();

        var verifyRequest = TestDataFactory.CreateVerifySmsCodeRequest(registerRequest.PhoneNumber, "123456");

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/verify-sms-code", verifyRequest, JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
