using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Application.Common.Settings;

public class DriverWalletOptions
{
    public const string SectionName = "DriverWallet";

    /// <summary>
    /// The percentage commission taken from each trip fare (e.g., 15.0 for 15%).
    /// </summary>
    [Range(0, 100)]
    public double CommissionPercentage { get; set; }

    /// <summary>
    /// The maximum debt limit allowed for a driver's wallet in EGP.
    /// </summary>
    [Range(0, 1000)]
    public decimal DebtLimitEgp { get; set; }
}
