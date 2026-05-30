using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Presentation.AppVersioning;

public class AppVersionSettings
{
    public const string SectionName = "AppVersioning";

    [Required]
    public required Settings Driver { get; set; }

    [Required]
    public required Settings Rider { get; set; }

    public class Settings
    {

        [Required]
        public required string MinimumVersion { get; set; }
        [Required]
        public required string LatestVersion { get; set; }

        public bool Ignore { get; set; } = false;

        public AppVersion MinimumAppVersion => AppVersion.Parse(MinimumVersion);
        public AppVersion LatestAppVersion => AppVersion.Parse(LatestVersion);
    }
}
