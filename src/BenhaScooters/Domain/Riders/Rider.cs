using BenhaScooters.Domain.Riders.ValueObjects;
using BenhaScooters.Domain.Users;
using Vogen;

namespace BenhaScooters.Domain.Riders;

[ValueObject<int>]
public partial struct RiderId;

public class Rider
{
    public RiderId Id { get; private set; }
    public UserId UserId { get; private set; }

    //  information
    public string? PreferredName { get; private set; }
    public RiderRating Rating { get; private set; } = null!;
    
    // Payment and booking
    public string? DefaultPaymentMethodId { get; private set; }
    public List<string> SavedAddresses { get; private set; } = new();
    
    // Status and metrics
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastTripAt { get; private set; }
    public int TotalTrips { get; private set; } = 0;

    // Navigation Properties
    public User User { get; private set; } = null!;

    private Rider() { } // For EF Core

    public Rider(UserId userId, string? preferredName = null)
    {
        UserId = userId;
        PreferredName = preferredName;
        Rating = RiderRating.Create();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferredName(string? preferredName)
    {
        PreferredName = preferredName?.Trim();
    }

    public void SetDefaultPaymentMethod(string paymentMethodId)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodId))
            throw new ArgumentException("Payment method ID cannot be empty", nameof(paymentMethodId));

        DefaultPaymentMethodId = paymentMethodId;
    }

    public void AddSavedAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        var trimmedAddress = address.Trim();
        if (!SavedAddresses.Contains(trimmedAddress))
        {
            SavedAddresses.Add(trimmedAddress);
        }
    }

    public void RemoveSavedAddress(string address)
    {
        SavedAddresses.Remove(address);
    }

    public void AddRating(decimal newRating)
    {
        Rating.AddRating(newRating);
    }

    public void RecordTripCompleted()
    {
        TotalTrips++;
        LastTripAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    // Calculated properties
    public bool IsNewRider => TotalTrips == 0;
    public bool IsFrequentRider => TotalTrips >= 10;
}

