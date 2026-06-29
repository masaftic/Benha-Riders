using System.Globalization;

namespace BenhaScooters.Shared.Localization;

public static class AppLanguages
{
    public const string English = "en";
    public const string Arabic = "ar";

    public static readonly CultureInfo[] SupportedCultures =
    [
        new(English),
        new(Arabic)
    ];

    public static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var normalized = language.Trim().ToLowerInvariant();

        var separatorIndex = normalized.IndexOf('-');
        if (separatorIndex > 0)
        {
            normalized = normalized[..separatorIndex];
        }

        return normalized is English or Arabic
            ? normalized
            : null;
    }

    public static bool IsSupported(string? language) => Normalize(language) is not null;
}
