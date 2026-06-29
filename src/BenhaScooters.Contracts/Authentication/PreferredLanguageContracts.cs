using BenhaScooters.Shared.Localization;
using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record UpdatePreferredLanguageRequest(string Language);

public class UpdatePreferredLanguageRequestValidator : AbstractValidator<UpdatePreferredLanguageRequest>
{
    public UpdatePreferredLanguageRequestValidator()
    {
        RuleFor(x => x.Language)
            .Must(AppLanguages.IsSupported)
            .WithMessage("Language must be either 'en' or 'ar'.");
    }
}
