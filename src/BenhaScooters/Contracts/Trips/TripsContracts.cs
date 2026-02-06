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
