using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class TripErrors
{
    public static class TripRequest
    {
        public static readonly Error NotFound = Error.NotFound(
            "TRIP_REQUEST_NOTFOUND",
            "Trip request not found");

        public static readonly Error AlreadyCancelled = Error.Conflict(
            "TRIP_REQUEST_ALREADY_CANCELLED",
            "Trip request is already cancelled");

        public static readonly Error AlreadyMatched = Error.Conflict(
            "TRIP_REQUEST_ALREADY_MATCHED",
            "Trip has already started and cannot be cancelled");

        public static readonly Error NotPending = Error.Conflict(
            "TRIP_REQUEST_NOT_PENDING",
            "Trip request is no longer available");

        public static readonly Error Expired = Error.Conflict(
            "TRIP_REQUEST_EXPIRED",
            "Trip request has expired");

        public static readonly Error InvalidCoordinates = Error.Validation(
            "TRIP_REQUEST_INVALID_COORDINATES",
            "Invalid pickup or dropoff coordinates");
    }

    public static class Trip
    {
        public static readonly Error NotFound = Error.NotFound(
            "TRIP_NOT_FOUND",
            "Trip not found or you are not the assigned driver");

        public static readonly Error RiderNotFound = Error.NotFound(
            "TRIP_RIDER_NOT_FOUND",
            "Trip not found for this rider");

        public static readonly Error NotInProgress = Error.Conflict(
            "TRIP_NOT_IN_PROGRESS",
            "Can only update GPS location for trips in progress");

        public static readonly Error CannotStart = Error.Conflict(
            "TRIP_CANNOT_START",
            "Trip cannot be started in current state");

        public static readonly Error CannotComplete = Error.Conflict(
            "TRIP_CANNOT_COMPLETE",
            "Trip cannot be completed in current state");

        public static readonly Error DriverNotArrived = Error.Conflict(
            "TRIP_DRIVER_NOT_ARRIVED",
            "Driver must arrive at pickup location first");

        public static readonly Error InvalidGpsCoordinates = Error.Validation(
            "TRIP_INVALID_GPS_COORDINATES",
            "Invalid GPS coordinates provided");

        public static readonly Error InvalidStatus = Error.Conflict(
            "TRIP_INVALID_STATUS",
            "Cannot perform this operation with current trip status");

        public static readonly Error FareAlreadySet = Error.Conflict(
            "TRIP_FARE_ALREADY_SET",
            "Trip fare has already been set");
    }

    public static class Driver
    {
        public static readonly Error NotAvailable = Error.Conflict(
            "DRIVER_NOT_AVAILABLE",
            "Driver is not available for trips");

        public static readonly Error AlreadyOnTrip = Error.Conflict(
            "DRIVER_ALREADY_ON_TRIP",
            "Driver is already on a trip");

        public static readonly Error NotOnline = Error.Conflict(
            "DRIVER_NOT_ONLINE",
            "Driver must be online to see available trips");
    }

    public static class Rider
    {
        public static readonly Error ProfileNotFound = Error.NotFound(
            "RIDER_PROFILE_NOT_FOUND",
            "Rider profile not found");

        public static readonly Error HasActiveTripRequest = Error.Conflict(
            "RIDER_HAS_ACTIVE_TRIP_REQUEST",
            "Rider already has an active trip request");
    }
}
