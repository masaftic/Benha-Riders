namespace BenhaScooters.Domain.Common;

public static partial class AppErrors
{
    public static class Matching
    {
        public static class Session
        {
            public static AppError NotActive() => NewValidation(
                "MATCHING_SESSION_NOT_ACTIVE",
                "Matching session is not active.");

            public static AppError Expired() => NewValidation(
                "MATCHING_SESSION_EXPIRED",
                "Matching session has expired.");

            public static AppError NotFound() => NewNotFound(
                "MATCHING_SESSION_NOT_FOUND",
                "Matching session was not found.");

            public static AppError InvalidNumberOfRounds() => NewValidation(
                "MATCHING_SESSION_INVALID_NUMBER_OF_ROUNDS",
                "Number of rounds must be greater than zero.");

            public static AppError InvalidOffersPerRoundCount() => NewValidation(
                "MATCHING_SESSION_INVALID_OFFERS_PER_ROUND_COUNT",
                "Offers-per-round count must match the configured number of rounds.");

            public static AppError InvalidOffersPerRoundValue() => NewValidation(
                "MATCHING_SESSION_INVALID_OFFERS_PER_ROUND_VALUE",
                "All offers-per-round values must be greater than zero.");
        }

        public static class Driver
        {
            public static AppError NotAvailable() => NewValidation(
                "DRIVER_NOT_AVAILABLE_FOR_MATCHING",
                "Driver is not available for trip matching.");

            public static AppError AlreadyOffered() => NewValidation(
                "DRIVER_ALREADY_HAS_OFFER",
                "Driver already has a pending trip offer.");
        }

        public static class Attempt
        {
            public static AppError NotFound() => NewNotFound(
                "MATCH_ATTEMPT_NOT_FOUND",
                "Match attempt was not found.");

            public static AppError Expired() => NewValidation(
                "MATCH_ATTEMPT_EXPIRED",
                "Match attempt has expired.");

            public static AppError InvalidStatus() => NewValidation(
                "MATCH_ATTEMPT_INVALID_STATUS",
                "Match attempt is not in a valid state for this operation.");

            public static AppError CannotAcceptOwnRequest() => NewValidation(
                "MATCH_ATTEMPT_CANNOT_ACCEPT_OWN_REQUEST",
                "You cannot accept your own trip request.");
        }
    }
}
