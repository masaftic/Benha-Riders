// using BenhaScooters.Domain.Common;

// namespace BenhaScooters.Domain.Riders.ValueObjects;

// public class RiderPreferences : ValueObject
// {
//     public bool AllowSharedRides { get; private set; }
//     public bool AllowPhoneCalls { get; private set; }
//     public string? MusicPreference { get; private set; }
//     public string? TemperaturePreference { get; private set; }

//     private RiderPreferences() { } // For EF Core

//     public RiderPreferences(bool allowSharedRides, bool allowPhoneCalls, 
//         string? musicPreference = null, string? temperaturePreference = null)
//     {
//         AllowSharedRides = allowSharedRides;
//         AllowPhoneCalls = allowPhoneCalls;
//         MusicPreference = musicPreference?.Trim();
//         TemperaturePreference = temperaturePreference?.Trim();
//     }

//     public static RiderPreferences Default()
//     {
//         return new RiderPreferences(
//             allowSharedRides: false,
//             allowPhoneCalls: true,
//             musicPreference: null,
//             temperaturePreference: null
//         );
//     }

//     protected override IEnumerable<object> GetEqualityComponents()
//     {
//         yield return AllowSharedRides;
//         yield return AllowPhoneCalls;
//         yield return MusicPreference ?? string.Empty;
//         yield return TemperaturePreference ?? string.Empty;
//     }
// }
