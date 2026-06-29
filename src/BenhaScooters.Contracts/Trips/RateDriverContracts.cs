using FluentValidation;

namespace BenhaScooters.Contracts.Trips;

public record RateDriverRequest(int Rating, string? Comment);
public record RateRiderRequest(int Rating, string? Comment);

public class RateDriverRequestValidator : AbstractValidator<RateDriverRequest>
{
    public RateDriverRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("التقييم يجب أن يكون بين 1 و 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("التعليق لا يمكن أن يتجاوز 500 حرف.")
            .When(x => x.Comment != null);
    }
}

public class RateRiderRequestValidator : AbstractValidator<RateRiderRequest>
{
    public RateRiderRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("التقييم يجب أن يكون بين 1 و 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("التعليق لا يمكن أن يتجاوز 500 حرف.")
            .When(x => x.Comment != null);
    }
}
