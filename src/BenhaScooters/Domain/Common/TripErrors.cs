namespace BenhaScooters.Domain.Common;

public static partial class AppErrors
{
    public static class TripRequest
    {
        public static AppError NotFound() => NewNotFound(
            "TRIP_REQUEST_NOT_FOUND",
            "Trip request was not found.");

        public static AppError AlreadyMatched() => NewConflict(
            "TRIP_REQUEST_ALREADY_MATCHED",
            "Trip request has already been matched.");

        public static AppError NotPending() => NewConflict(
            "TRIP_REQUEST_NOT_PENDING",
            "Trip request is no longer pending.");

        public static AppError Expired() => NewConflict(
            "TRIP_REQUEST_EXPIRED",
            "Trip request has expired.");

        public static AppError Forbidden() => NewForbidden(
            "TRIP_REQUEST_FORBIDDEN",
            "You are not allowed to confirm this trip request.");

        public static AppError AlreadyConfirmed() => NewConflict(
            "TRIP_REQUEST_ALREADY_CONFIRMED",
            "Trip request has already been confirmed.");
    }

    public static class Trip
    {
        public static AppError NotFound() => NewNotFound(
            "TRIP_NOT_FOUND",
            "Trip was not found.");

        public static AppError InvalidStatus() => NewConflict(
            "TRIP_INVALID_STATUS",
            "This action is not allowed for the trip in its current status.");

        public static AppError PaymentAlreadySet() => NewConflict(
            "TRIP_PAYMENT_ALREADY_SET",
            "Payment has already been set for this trip.");

        public static AppError PaymentNotSet() => NewConflict(
            "TRIP_PAYMENT_NOT_SET",
            "Payment has not been set for this trip.");

        public static AppError CannotCancel() => NewConflict(
            "TRIP_CANNOT_CANCEL",
            "This trip can no longer be cancelled.");
    }

    public static class Rider
    {
        public static AppError HasActiveTripRequest() => NewConflict(
            "RIDER_HAS_ACTIVE_TRIP_REQUEST",
            "You already have an active trip request.");

        public static AppError HasActiveTrip() => NewConflict(
            "RIDER_HAS_ACTIVE_TRIP",
            "You already have an active trip.");
    }

    public static class ServiceArea
    {
        public static AppError LocationNotCovered() => NewConflict(
            "SERVICE_AREA_LOCATION_NOT_COVERED",
            "The pickup or dropoff location is outside the supported service area.");
    }

    public static class Payment
    {
        public static AppError AlreadyPaid() => NewConflict(
            "PAYMENT_ALREADY_PAID",
            "The payment has already been marked as paid.");

        public static AppError InvalidPaidAmount() => NewValidation(
            "INVALID_PAID_AMOUNT",
            "Paid amount must be greater than zero.");

        public static AppError InsufficientAmount() => NewValidation(
            "INSUFFICIENT_PAYMENT",
            "Paid amount is less than the required amount.");
    }

    public static class Rating
    {
        public static AppError InvalidValue() => NewValidation(
            "INVALID_RATING",
            "Rating must be between 1 and 5.");

        public static AppError TripNotCompleted() => NewValidation(
            "TRIP_NOT_COMPLETED",
            "You can rate the driver only after the trip is completed.");

        public static AppError AlreadySubmitted() => NewConflict(
            "ALREADY_RATED",
            "You have already rated this trip.");

        public static AppError NotYourTrip() => NewForbidden(
            "NOT_YOUR_TRIP",
            "You cannot rate a trip that you were not part of.");
    }
}
