using System.Text.Json.Serialization;

namespace BenhaScooters.Domain.Users;

public enum App
{
    DriverApp,
    RiderApp
}

public static class AppExtensions
{
    public const string DriverAppValue = "driver-app";
    public const string RiderAppValue = "rider-app";

    public static string ToClaimValue(this App app) => app switch
    {
        App.DriverApp => DriverAppValue,
        App.RiderApp => RiderAppValue,
        _ => throw new ArgumentOutOfRangeException(nameof(app), app, "Unknown app")
    };

    public static App FromClaimValue(string value) => value switch
    {
        DriverAppValue => App.DriverApp,
        RiderAppValue => App.RiderApp,
        _ => throw new ArgumentException($"Unknown app value: {value}", nameof(value))
    };
}
