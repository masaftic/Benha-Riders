using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Infrastructure.GoogleMaps;

public class GoogleMapsOptions
{
    public const string SectionName = "GoogleMaps";

    [Required]    
    [MinLength(1)]
    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api";
}
