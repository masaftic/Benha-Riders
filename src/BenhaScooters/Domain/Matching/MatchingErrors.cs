using ErrorOr;

namespace BenhaScooters.Domain.Matching;

public static class MatchingErrors
{
    public static class Session
    {
        public static Error NotActive => Error.Validation(
            "MATCHING_SESSION_NOT_ACTIVE",
            "Matching session is not active");

        public static Error Expired => Error.Validation(
            "MATCHING_SESSION_EXPIRED", 
            "Matching session has expired");

        public static Error NotFound => Error.NotFound(
            "MATCHING_SESSION_NOT_FOUND",
            "Matching session not found");
    }

    public static class Driver
    {
        public static Error NotAvailable => Error.Validation(
            "DRIVER_NOT_AVAILABLE_FOR_MATCHING",
            "Driver is not available for trip matching");

        public static Error AlreadyOffered => Error.Validation(
            "DRIVER_ALREADY_HAS_OFFER",
            "Driver already has a pending trip offer");
    }
}
