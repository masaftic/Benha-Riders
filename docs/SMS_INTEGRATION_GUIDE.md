# SMS Integration Setup Guide

## Overview

The application now supports real SMS sending via **WhySMS API** with comprehensive security features including:

- ✅ Real SMS delivery via WhySMS API
- ✅ Dummy SMS service for testing/development
- ✅ Fixed OTP codes for testing (bypass random generation)
- ✅ Full audit logging of all SMS sends
- ✅ Configurable via appsettings.json and User Secrets

---

## Configuration

### 1. **appsettings.json** (Development/Testing)

Already configured in `appsettings.json`:

```json
{
  "Sms": {
    "ApiUrl": "https://bulk.whysms.com/api/v3/sms/send",
    "ApiKey": "YOUR_API_KEY_FROM_USER_SECRETS",
    "SenderId": "YOUR_SENDER_ID_FROM_USER_SECRETS",
    "EnableRealSmsSending": false,  // Set to true to enable real SMS
    "RequestTimeoutSeconds": 30
  },
  "OtpSecurity": {
    // ... existing options ...
    "UseFixedOtp": true,         // Use fixed OTP for testing
    "FixedOtpCode": "123456"     // The fixed code to use
  }
}
```

### 2. **User Secrets** (Production/Sensitive Data)

Store your WhySMS API credentials in User Secrets:

```bash
cd src/BenhaScooters
dotnet user-secrets set "Sms:ApiKey" "your-whysms-api-key-here"
dotnet user-secrets set "Sms:SenderId" "your-sender-id-here"
```

---

## How It Works

### **Testing Mode** (Current Default)
- `EnableRealSmsSending = false`
- `UseFixedOtp = true`
- Uses `DummySmsService` → Logs SMS to console only
- Always generates OTP `123456`
- **Perfect for development and testing OTP security flows**

### **Production Mode**
- `EnableRealSmsSending = true`
- `UseFixedOtp = false`
- Uses `WhySmsSenderService` → Sends real SMS via WhySMS API
- Generates cryptographically secure random 6-digit OTP
- Logs all SMS sends to `SmsLogs` table

---

## Database Migration

Run this migration to add the `sms_logs` table:

```bash
cd src/BenhaScooters
dotnet ef migrations add AddSmsLogTable
dotnet ef database update
```

---

## SMS Logging

All SMS sends (real or dummy) are logged to the `SmsLogs` table with:

- **User ID** (if authenticated)
- **Phone number**
- **SMS type** (Verification, General, Notification)
- **Message content**
- **Provider response** (ID, UID, cost, SMS count)
- **Delivery status** (Pending, Delivered, Failed, Rejected)
- **IP address** (for fraud tracking)
- **Timestamps** (created, delivered)
- **Error messages** (if failed)

Query example:
```csharp
var recentSms = await _db.SmsLogs
    .Where(s => s.UserId == userId)
    .OrderByDescending(s => s.CreatedAt)
    .Take(10)
    .ToListAsync();
```

---

## Testing Scenarios

### Scenario 1: Test OTP Security (No Real SMS)
```json
{
  "Sms": { "EnableRealSmsSending": false },
  "OtpSecurity": { "UseFixedOtp": true, "FixedOtpCode": "123456" }
}
```
- User requests SMS → Logs to console: `🔐 [DUMMY SMS VERIFICATION] Code: 123456`
- User enters `123456` → Verified successfully ✓

### Scenario 2: Test with Random OTPs (No Real SMS)
```json
{
  "Sms": { "EnableRealSmsSending": false },
  "OtpSecurity": { "UseFixedOtp": false }
}
```
- User requests SMS → Logs to console: `🔐 [DUMMY SMS VERIFICATION] Code: 382947`
- User enters the logged code → Verified successfully ✓

### Scenario 3: Production (Real SMS)
```json
{
  "Sms": { "EnableRealSmsSending": true, "ApiKey": "real-key", "SenderId": "real-id" },
  "OtpSecurity": { "UseFixedOtp": false }
}
```
- User requests SMS → Real SMS sent via WhySMS API
- SMS delivery tracked in `SmsLogs` table
- User enters received code → Verified successfully ✓

---

## WhySMS API Integration

### Request Format
```http
POST https://bulk.whysms.com/api/v3/sms/send
Authorization: Bearer {ApiKey}
Content-Type: application/json

{
  "recipient": "+20xxxxxxxx",
  "sender_id": "YourSenderID",
  "type": "OTP",
  "message": "رمز التحقق الخاص بك هو: 123456. صالح لمدة 5 دقائق."
}
```

### Success Response
```json
{
  "status": "success",
  "message": "Your message was successfully delivered",
  "data": {
    "id": 11738283,
    "uid": "69a4acbedc67d",
    "to": "+20xxx",
    "from": "YourSenderID",
    "message": "رمز التحقق...",
    "status": "Delivered",
    "cost": 1,
    "sms_count": 1,
    "media_url": null
  }
}
```

### Error Response
```json
{
  "status": "error",
  "message": "A human-readable description of the error."
}
```

---

## Architecture

```
AuthenticationController
    ↓
SendSmsVerificationCommand
    ↓
OtpSecurityService.RequestCodeAsync()
    ├─ Fraud Detection
    ├─ Rate Limiting
    ├─ Generate OTP (fixed or random)
    └─ Return plaintext code
        ↓
ISmsService.SendVerificationCodeAsync()
    ├─ If EnableRealSmsSending = true → WhySmsSenderService
    │   ├─ HTTP POST to WhySMS API
    │   ├─ Parse response
    │   └─ Save to SmsLogs table
    │
    └─ If EnableRealSmsSending = false → DummySmsService
        └─ Log to console only
```

---

## Troubleshooting

### Issue: "SMS not being sent"
- Check `EnableRealSmsSending` is `true` in configuration
- Verify API key and sender ID in User Secrets
- Check logs for HTTP errors
- Inspect `SmsLogs` table for error messages

### Issue: "Invalid verification code"
- If `UseFixedOtp = true`, code should always be `123456`
- If `UseFixedOtp = false`, check console logs for the generated code (in dummy mode)
- Code expires after 5 minutes (configurable via `CodeExpiryMinutes`)

### Issue: "Rate limit errors"
- Check `OtpSecurity` settings in appsettings.json
- Inspect `OtpSecurityEvents` table for rate limit triggers
- Adjust cooldown/limits if needed for testing

---

## Security Features

✅ **HMAC-SHA256 hashing** of OTP codes (never stored in plaintext)  
✅ **Cryptographic RNG** for random OTP generation  
✅ **Constant-time comparison** to prevent timing attacks  
✅ **Rate limiting** (6 separate limits: user, IP, phone × hourly/daily)  
✅ **Progressive cooldowns** (60s → 30 min as attempts increase)  
✅ **Fraud detection** (IP enumeration, credential sharing)  
✅ **Brute-force protection** (max 5 attempts per code, lockout after 15 fails)  
✅ **Audit logging** (all OTP events + all SMS sends)  
✅ **IP tracking** for fraud analysis

---

## Next Steps

1. **Test in development**:
   - Use dummy mode with fixed OTP `123456`
   - Verify OTP security flow works end-to-end

2. **Get WhySMS credentials**:
   - Sign up at WhySMS
   - Obtain API key and sender ID
   - Add to User Secrets

3. **Test with real SMS**:
   - Set `EnableRealSmsSending = true`
   - Set `UseFixedOtp = false`
   - Test with your phone number

4. **Deploy to production**:
   - Ensure `UseFixedOtp = false`
   - Ensure `EnableRealSmsSending = true`
   - Use environment variables or Azure Key Vault for secrets
