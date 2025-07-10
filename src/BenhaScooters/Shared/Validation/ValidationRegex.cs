namespace BenhaScooters.Shared.Validation;

public static class ValidationRegex
{
    public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    public const string PhoneNumber = @"^(\+20|0)?[1-9][0-9]{9}$"; // Basic validation for Egyptian phone numbers
    public const string NationalId = @"^[0-9]{14}$"; // Egyptian national ID validation (14 digits)
    public const string LicensePlate = @"^[أ-ي]{3}\s?\d{4}$"; // Egyptian license plate
    // public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,}$"; // At least 8 characters, 1 uppercase, 1 lowercase, 1 number
}