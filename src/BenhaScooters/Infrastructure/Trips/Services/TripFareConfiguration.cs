using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Infrastructure.Trips.Services;

public class TripFareConfiguration
{
    public const string SectionName = "TripFare";

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Base fare must be a non-negative value.")]
    public decimal BaseFare { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price per kilometer must be a non-negative value.")]
    public decimal PricePerKm { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price per minute must be a non-negative value.")]
    public decimal PricePerMinute { get; set; }
}