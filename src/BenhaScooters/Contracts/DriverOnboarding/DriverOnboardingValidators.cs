using BenhaScooters.Contracts.DriverOnboarding;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using FluentValidation;

namespace BenhaScooters.Contracts.DriverOnboarding;

public class UpdatePersonalInfoRequestValidator : AbstractValidator<UpdatePersonalInfoRequest>
{
    public UpdatePersonalInfoRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("الاسم الكامل مطلوب.")
            .MinimumLength(2)
            .WithMessage("الاسم الكامل يجب أن يكون على الأقل حرفين.")
            .MaximumLength(100)
            .WithMessage("الاسم الكامل لا يمكن أن يتجاوز 100 حرف.");

        RuleFor(x => x.NationalId)
            .NotEmpty()
            .WithMessage("الرقم القومي مطلوب.")
            .Matches(@"^[0-9]{14}$")
            .WithMessage("الرقم القومي يجب أن يكون 14 رقم بالضبط.");
    }
}

public class UpdateVehicleInfoRequestValidator : AbstractValidator<UpdateVehicleInfoRequest>
{
    public UpdateVehicleInfoRequestValidator()
    {
        RuleFor(x => x.VehicleType)
            .IsInEnum()
            .WithMessage("نوع المركبة صحيح مطلوب.");

        RuleFor(x => x.VehicleBrand)
            .NotEmpty()
            .WithMessage("ماركة المركبة مطلوبة.")
            .MinimumLength(2)
            .WithMessage("ماركة المركبة يجب أن تكون على الأقل حرفين.")
            .MaximumLength(50)
            .WithMessage("ماركة المركبة لا يمكن أن تتجاوز 50 حرف.");

        RuleFor(x => x.VehicleColor)
            .NotEmpty()
            .WithMessage("لون المركبة مطلوب.")
            .MinimumLength(2)
            .WithMessage("لون المركبة يجب أن يكون على الأقل حرفين.")
            .MaximumLength(30)
            .WithMessage("لون المركبة لا يمكن أن يتجاوز 30 حرف.");

        RuleFor(x => x.LicensePlate)
            .NotEmpty()
            .WithMessage("لوحة الترخيص مطلوبة.")
            .Length(3, 10)
            .WithMessage("لوحة الترخيص يجب أن تكون بين 3 و 10 أحرف.");

        RuleFor(x => x.VehicleYear)
            .InclusiveBetween(1980, DateTime.Now.Year + 1)
            .WithMessage($"سنة المركبة يجب أن تكون بين 1980 و {DateTime.Now.Year + 1}.");
    }
}

public class UpdateDocumentsRequestValidator : AbstractValidator<UpdateDocumentsRequest>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public UpdateDocumentsRequestValidator()
    {
        RuleFor(x => x.LicenseImage)
            .NotNull().WithMessage("صورة الرخصة مطلوبة.")
            .Must(BeAValidImageFile).WithMessage("صورة الرخصة يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");

        RuleFor(x => x.VehicleRegistrationImage)
            .NotNull().WithMessage("صورة تسجيل المركبة مطلوبة.")
            .Must(BeAValidImageFile).WithMessage("صورة تسجيل المركبة يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");

        RuleFor(x => x.DriverImage)
            .NotNull().WithMessage("صورة السائق مطلوبة.")
            .Must(BeAValidImageFile).WithMessage("صورة السائق يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");
    }

    private static bool BeAValidImageFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > MaxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}

public class UploadDocumentRequestValidator : AbstractValidator<UploadDocumentRequest>
{
    public UploadDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentType).NotEmpty().IsEnumName(typeof(DocumentType), caseSensitive: false).WithMessage("نوع المستند غير صحيح.");
        RuleFor(x => x.File).NotNull().WithMessage("الملف مطلوب.").Must(file => file.Length > 0).WithMessage("الملف لا يمكن أن يكون فارغ.");
    }
}

public class BanDriverRequestValidator : AbstractValidator<BanDriverRequest>
{
    public BanDriverRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Ban reason is required.")
            .MaximumLength(500).WithMessage("Ban reason must not exceed 500 characters.");
    }
}

public class QueryDriversParamsValidator : AbstractValidator<QueryDriversParams>
{
    public QueryDriversParamsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageCount)
            .GreaterThan(0).WithMessage("Page count must be greater than 0.");
    }
}
