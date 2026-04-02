namespace BenhaScooters.Domain.Common;

public static partial class AppErrors
{
    public static class Storage
    {
        public static AppError InvalidFileType(string extension, params string[] allowedExtensions) => NewValidation(
            "INVALID_FILE_TYPE",
            "This file type is not allowed.",
            ("extension", extension),
            ("allowedExtensions", allowedExtensions));

        public static AppError InvalidFileSize(long maxFileSizeBytes) => NewValidation(
            "INVALID_FILE_SIZE",
            "File size exceeds the maximum allowed limit.",
            ("maxFileSizeBytes", maxFileSizeBytes));

        public static AppError UploadFailed() => NewFailure(
            "FILE_UPLOAD_FAILED",
            "File upload failed.");
    }

    public static class GoogleMaps
    {
        public static class Geocode
        {
            public static AppError NullResponse() => NewFailure(
                "GOOGLE_MAPS_GEOCODE_NULL_RESPONSE",
                "Unable to retrieve location details right now.");

            public static AppError HttpError() => NewFailure(
                "GOOGLE_MAPS_GEOCODE_HTTP_ERROR",
                "Unable to retrieve location details right now.");

            public static AppError Unexpected() => NewUnexpected(
                "GOOGLE_MAPS_GEOCODE_UNEXPECTED_ERROR",
                "An unexpected location lookup error occurred.");
        }

        public static class Autocomplete
        {
            public static AppError NullResponse() => NewFailure(
                "GOOGLE_MAPS_AUTOCOMPLETE_NULL_RESPONSE",
                "Unable to load address suggestions right now.");

            public static AppError HttpError() => NewFailure(
                "GOOGLE_MAPS_AUTOCOMPLETE_HTTP_ERROR",
                "Unable to load address suggestions right now.");

            public static AppError Unexpected() => NewUnexpected(
                "GOOGLE_MAPS_AUTOCOMPLETE_UNEXPECTED_ERROR",
                "An unexpected address suggestion error occurred.");
        }

        public static class PlaceDetails
        {
            public static AppError NullResponse() => NewFailure(
                "GOOGLE_MAPS_PLACE_DETAILS_NULL_RESPONSE",
                "Unable to load place details right now.");

            public static AppError HttpError() => NewFailure(
                "GOOGLE_MAPS_PLACE_DETAILS_HTTP_ERROR",
                "Unable to load place details right now.");

            public static AppError Unexpected() => NewUnexpected(
                "GOOGLE_MAPS_PLACE_DETAILS_UNEXPECTED_ERROR",
                "An unexpected place details error occurred.");
        }

        public static class Directions
        {
            public static AppError NullResponse() => NewFailure(
                "GOOGLE_MAPS_DIRECTIONS_NULL_RESPONSE",
                "Unable to load directions right now.");

            public static AppError HttpError() => NewFailure(
                "GOOGLE_MAPS_DIRECTIONS_HTTP_ERROR",
                "Unable to load directions right now.");

            public static AppError Unexpected() => NewUnexpected(
                "GOOGLE_MAPS_DIRECTIONS_UNEXPECTED_ERROR",
                "An unexpected directions error occurred.");
        }
    }
}
