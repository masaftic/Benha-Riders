using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using Thinktecture;

namespace BenhaScooters.Domain.Trips.ValueObjects;

[ComplexValueObject]
public partial class FareEstimate
{
    public decimal Amount { get; }
    public Distance Distance { get; } // kilometers
    public Duration Time { get; } // minutes
}
