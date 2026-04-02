namespace BenhaScooters.Domain.Common;

public static partial class AppErrors
{
    public static class Admin
    {
        public static AppError DriverNotFound() => NewNotFound(
            "ADMIN_DRIVER_NOT_FOUND",
            "Driver was not found.");
    }
}
