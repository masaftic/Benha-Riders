using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Infrastructure.Trips.Services;

public class FareEstimationOptions
{
    public const string SectionName = "FareEstimation";
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Average speed must be a non-negative value.")]
    public double AverageSpeedKmh { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Base fare must be a non-negative value.")]
    public decimal BaseFare { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Per kilometer rate must be a non-negative value.")]
    public decimal PerKmRate { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Per minute rate must be a non-negative value.")]
    public decimal PerMinuteRate { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Minimum fare must be a non-negative value.")]
    public decimal MinimumFare { get; set; }
}
