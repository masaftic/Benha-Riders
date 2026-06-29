using FluentValidation;

namespace BenhaScooters.Contracts.Trips;

public record PayCashForTripRequest(decimal PaidAmount);

public class PayCashForTripRequestValidator : AbstractValidator<PayCashForTripRequest>
{
    public PayCashForTripRequestValidator()
    {
        RuleFor(x => x.PaidAmount)
            .GreaterThan(0).WithMessage("Paid amount must be greater than zero.");
    }
}


public record CancelTripRequest(string CancellationReason);

public class CancelTripRequestValidator : AbstractValidator<CancelTripRequest>
{
    public CancelTripRequestValidator()
    {
        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("طلب إلغاء الرحلة يتطلب سببًا.")
            .MaximumLength(500).WithMessage("سبب الإلغاء لا يمكن أن يتجاوز 500 حرف.");
    }
}