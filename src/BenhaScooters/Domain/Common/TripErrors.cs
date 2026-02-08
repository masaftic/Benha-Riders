using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class TripErrors
{
    public static class TripRequest
    {
        public static readonly Error NotFound = Error.NotFound(
            "TRIP_REQUEST_NOTFOUND",
            "طلب الرحلة غير موجود");

        public static readonly Error AlreadyCancelled = Error.Conflict(
            "TRIP_REQUEST_ALREADY_CANCELLED",
            "طلب الرحلة ملغي بالفعل");

        public static readonly Error AlreadyMatched = Error.Conflict(
            "TRIP_REQUEST_ALREADY_MATCHED",
            "الرحلة بدأت بالفعل ولا يمكن إلغاؤها");

        public static readonly Error NotPending = Error.Conflict(
            "TRIP_REQUEST_NOT_PENDING",
            "طلب الرحلة لم يعد متاحاً");

        public static readonly Error Expired = Error.Conflict(
            "TRIP_REQUEST_EXPIRED",
            "انتهت صلاحية طلب الرحلة");

        public static readonly Error InvalidCoordinates = Error.Validation(
            "TRIP_REQUEST_INVALID_COORDINATES",
            "إحداثيات نقطة الانطلاق أو الوصول غير صحيحة");
        
        public static readonly Error Forbidden = Error.Forbidden(
            "TRIP_REQUEST_FORBIDDEN",
            "غير مصرح لك بتأكيد طلب الرحلة هذا");
        
        public static readonly Error AlreadyConfirmed = Error.Conflict(
            "TRIP_REQUEST_ALREADY_CONFIRMED",
            "تم تأكيد طلب الرحلة بالفعل");
    }

    public static class Trip
    {
        public static readonly Error NotFound = Error.NotFound(
            "TRIP_NOT_FOUND",
            "الرحلة غير موجودة أو أنت لست السائق المخصص");

        public static readonly Error RiderNotFound = Error.NotFound(
            "TRIP_RIDER_NOT_FOUND",
            "الرحلة غير موجودة لهذا الراكب");

        public static readonly Error NotInProgress = Error.Conflict(
            "TRIP_NOT_IN_PROGRESS",
            "يمكن تحديث موقع GPS فقط للرحلات قيد التنفيذ");

        public static readonly Error CannotStart = Error.Conflict(
            "TRIP_CANNOT_START",
            "لا يمكن بدء الرحلة في الحالة الحالية");

        public static readonly Error CannotComplete = Error.Conflict(
            "TRIP_CANNOT_COMPLETE",
            "لا يمكن إنهاء الرحلة في الحالة الحالية");

        public static readonly Error DriverNotArrived = Error.Conflict(
            "TRIP_DRIVER_NOT_ARRIVED",
            "يجب على السائق الوصول إلى نقطة الانطلاق أولاً");

        public static readonly Error InvalidGpsCoordinates = Error.Validation(
            "TRIP_INVALID_GPS_COORDINATES",
            "إحداثيات GPS المقدمة غير صحيحة");

        public static readonly Error InvalidStatus = Error.Conflict(
            "TRIP_INVALID_STATUS",
            "لا يمكن تنفيذ هذه العملية مع حالة الرحلة الحالية");

        public static readonly Error FareAlreadySet = Error.Conflict(
            "TRIP_FARE_ALREADY_SET",
            "تم تحديد تكلفة الرحلة بالفعل");

        public static readonly Error PaymentAlreadySet = Error.Conflict(
            "TRIP_PAYMENT_ALREADY_SET",
            "تم تحديد طريقة الدفع للرحلة بالفعل");
        
        public static readonly Error PaymentNotSet = Error.Conflict(
            "TRIP_PAYMENT_NOT_SET",
            "لم يتم تحديد طريقة الدفع للرحلة بعد");
    }

    public static class Driver
    {
        public static readonly Error NotAvailable = Error.Conflict(
            "DRIVER_NOT_AVAILABLE",
            "السائق غير متاح للرحلات");

        public static readonly Error AlreadyOnTrip = Error.Conflict(
            "DRIVER_ALREADY_ON_TRIP",
            "السائق في رحلة بالفعل");

        public static readonly Error NotOnline = Error.Conflict(
            "DRIVER_NOT_ONLINE",
            "يجب أن يكون السائق متصلاً لرؤية الرحلات المتاحة");
    }

    public static class Rider
    {
        public static readonly Error ProfileNotFound = Error.NotFound(
            "RIDER_PROFILE_NOT_FOUND",
            "ملف الراكب غير موجود");

        public static readonly Error HasActiveTripRequest = Error.Conflict(
            "RIDER_HAS_ACTIVE_TRIP_REQUEST",
            "الراكب لديه طلب رحلة نشط بالفعل");
        
        public static readonly Error HasActiveTrip = Error.Conflict(
            "RIDER_HAS_ACTIVE_TRIP",
            "الراكب في رحلة نشطة بالفعل");
    }
}
