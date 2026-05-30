namespace BenhaScooters.Presentation.AppVersioning;

public record AppVersion(int Major, int Minor, int Patch, int Build = 0) : IComparable<AppVersion>
{
    /// <summary>
    /// Compares this version instance with another AppVersion.
    /// </summary>
    public int CompareTo(AppVersion? other)
    {
        if (other is null) return 1;

        // Compare step-by-step from most significant to least significant
        int majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        int minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        int patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        return Build.CompareTo(other.Build);
    }

    // Overloading operators allows using >, <, >=, <= directly in your code
    public static bool operator <(AppVersion left, AppVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(AppVersion left, AppVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(AppVersion left, AppVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(AppVersion left, AppVersion right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Outputs the version in the x.x.x+x format.
    /// </summary>
    public override string ToString() => $"{Major}.{Minor}.{Patch}+{Build}";

    /// <summary>
    /// Parses a string in the format "x.x.x+x" into an AppVersion instance.
    /// </summary>
    public static AppVersion Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Version string cannot be empty.", nameof(input));

        // Split into the semantic version core and the build metadata via '+'
        var mainParts = input.Split('+');
        if (mainParts.Length > 2)
            throw new FormatException("Invalid version format. Expected x.x.x+x");

        // Split the core into Major.Minor.Patch
        var semVerParts = mainParts[0].Split('.');
        if (semVerParts.Length != 3)
            throw new FormatException("Invalid version format. Core version must have Major.Minor.Patch sections.");

        if (!int.TryParse(semVerParts[0], out int major) ||
            !int.TryParse(semVerParts[1], out int minor) ||
            !int.TryParse(semVerParts[2], out int patch))
        {
            throw new FormatException("Major, Minor, and Patch sections must be valid integers.");
        }

        int build = 0;
        if (mainParts.Length == 2 && !int.TryParse(mainParts[1], out build))
        {
            throw new FormatException("Build section must be a valid integer.");
        }

        return new AppVersion(major, minor, patch, build);
    }

    /// <summary>
    /// Safely attempts to parse a string into an AppVersion.
    /// </summary>
    public static bool TryParse(string input, out AppVersion? result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}
