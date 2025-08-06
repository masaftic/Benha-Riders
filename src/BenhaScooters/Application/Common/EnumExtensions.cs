using System.Text.RegularExpressions;

namespace BenhaScooters.Application.Common;

public static class EnumExtensions
{
    public static string ToKebabCase(this Enum enumValue)
    {
        // Convert PascalCase to kebab-case
        string name = enumValue.ToString();
        string kebabCase = Regex.Replace(name, "([a-z0-9]|(?<=[A-Z]))([A-Z])", "$1-$2").ToLower();
        return kebabCase;
    }
}

