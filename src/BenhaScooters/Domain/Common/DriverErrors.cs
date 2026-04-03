using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Domain.Common;

public static partial class AppErrors
{
    public static class Driver
    {
        public static AppError NotFound() => NewNotFound(
            "DRIVER_NOT_FOUND",
            "Driver was not found.");

        public static AppError UploadFailed() => NewFailure(
            "DRIVER_DOCUMENT_UPLOAD_FAILED",
            "Failed to upload driver documents.");

        public static AppError NotOnline() => NewConflict(
            "DRIVER_NOT_ONLINE",
            "Driver must be online to access available trips.");

        public static class Profile
        {
            public static AppError LicensePlateAlreadyExists() => NewConflict(
                "LICENSE_PLATE_ALREADY_EXISTS",
                "This license plate is already registered with another vehicle."); 

            public static AppError CannotModifyApprovedProfile() => NewConflict(
                "CANNOT_MODIFY_APPROVED_PROFILE",
                "Approved driver profiles cannot be modified.");

            public static AppError InvalidStatusTransition() => NewConflict(
                "DRIVER_PROFILE_INVALID_STATUS_TRANSITION",
                "This action is not allowed in the current onboarding status.");

            public static AppError PersonalInfoRequired() => NewValidation(
                "PERSONAL_INFO_REQUIRED",
                "Personal information must be completed first.");

            public static AppError VehicleInfoRequired() => NewValidation(
                "VEHICLE_INFO_REQUIRED",
                "Vehicle information must be completed first.");

            public static AppError DocumentsRequired() => NewValidation(
                "DOCUMENTS_REQUIRED",
                "All required documents must be uploaded first.");

            public static AppError RejectionReasonRequired() => NewValidation(
                "REJECTION_REASON_REQUIRED",
                "A rejection reason is required.");

            public static AppError SuspensionReasonRequired() => NewValidation(
                "SUSPENSION_REASON_REQUIRED",
                "A suspension reason is required.");

            public static AppError NotFound() => NewNotFound(
                "DRIVER_PROFILE_NOT_FOUND",
                "Driver profile was not found.");
        }

        public static class Document
        {
            public static AppError NotFound() => NewNotFound(
                "DOCUMENT_NOT_FOUND",
                "Document was not found.");
        }

        public static class Status
        {
            public static AppError CannotGoOnlineWhileOnTrip() => NewConflict(
                "CANNOT_GO_ONLINE_WHILE_ON_TRIP",
                "You cannot go online while on a trip.");

            public static AppError CannotGoOfflineWhileOnTrip() => NewConflict(
                "CANNOT_GO_OFFLINE_WHILE_ON_TRIP",
                "You cannot go offline while on a trip.");

            public static AppError NotOnTrip() => NewConflict(
                "NOT_ON_TRIP",
                "You are not currently on a trip.");

            public static AppError AlreadyOnTrip() => NewConflict(
                "ALREADY_ON_TRIP",
                "You are already on a trip.");

            public static AppError ManualOnTripTransitionNotAllowed() => NewValidation(
                "DRIVER_STATUS_MANUAL_ONTRIP_TRANSITION_NOT_ALLOWED",
                "Driver status cannot be manually set to OnTrip.");

            public static AppError InvalidAvailabilityStatus() => NewValidation(
                "INVALID_STATUS",
                "Invalid driver availability status.");

            public static AppError InvalidOperation() => NewValidation(
                "INVALID_OPERATION",
                "Driver status change is invalid.");
        }

        public static class Wallet
        {
            public static AppError NotFound() => NewNotFound(
                "WALLET_NOT_FOUND",
                "Driver wallet was not found.");

            public static AppError DebtLimitExceeded() => NewConflict(
                "WALLET_DEBT_LIMIT_EXCEEDED",
                "You cannot accept trips until your wallet balance is settled.");

            public static AppError InvalidAmount() => NewValidation(
                "WALLET_INVALID_AMOUNT",
                "Wallet amount is invalid.");

            public static AppError AdjustmentReasonRequired() => NewValidation(
                "WALLET_ADJUSTMENT_REASON_REQUIRED",
                "An adjustment reason is required.");

            public static AppError TopUpRequestAlreadyReviewed() => NewConflict(
                "WALLET_TOPUP_ALREADY_REVIEWED",
                "This top-up request has already been reviewed.");

            public static AppError RejectionReasonRequired() => NewValidation(
                "WALLET_TOPUP_REJECTION_REASON_REQUIRED",
                "A rejection reason is required.");

            public static AppError TopUpRequestNotFound() => NewNotFound(
                "WALLET_TOPUP_NOT_FOUND",
                "Top-up request was not found.");

            public static AppError PendingTopUpExists() => NewConflict(
                "WALLET_TOPUP_PENDING_EXISTS",
                "You already have a pending top-up request.");
        }
    }
}
