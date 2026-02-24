using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;
using Thinktecture;


namespace BenhaScooters.Domain.Riders;


/// <summary>
/// Rider profile containing preferences and stats.
/// Uses UserId as primary key (1:1 relationship with User).
/// </summary>
public class RiderProfile
{
    public UserId UserId { get; private set; }  // PK & FK to User
    
    // Preferences
    public string? PreferredName { get; private set; }
    public string? DefaultPaymentMethodId { get; private set; }
    
    // Saved addresses (owned collection)
    private readonly List<SavedAddress> _savedAddresses = [];
    public IReadOnlyList<SavedAddress> SavedAddresses => _savedAddresses.AsReadOnly();
    
    // Stats
    public decimal AverageRating { get; private set; }
    public int TotalRatings { get; private set; }
    public int TotalTrips { get; private set; }
    public DateTime? LastTripAt { get; private set; }
    
    // Status
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    public ICollection<TripRequest> TripRequests { get; set; } = [];


    private RiderProfile() { } // For EF Core

    public RiderProfile(UserId userId, string? preferredName = null)
    {
        UserId = userId;
        PreferredName = preferredName?.Trim();
        AverageRating = 5.0m;  // Benefit of the doubt for new riders
        TotalRatings = 1;
        TotalTrips = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public RiderProfile(User user, string? preferredName = null)
    {
        User = user;
        UserId = user.Id;
        PreferredName = preferredName?.Trim();
        AverageRating = 5.0m;  // Benefit of the doubt for new riders
        TotalRatings = 1;
        TotalTrips = 0;
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

    public void ClearDefaultPaymentMethod()
    {
        DefaultPaymentMethodId = null;
    }

    public void AddSavedAddress(string label, string address, double? latitude = null, double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        // Remove existing with same label
        var existing = _savedAddresses.FirstOrDefault(a => a.Label == label);
        if (existing != null)
            _savedAddresses.Remove(existing);

        _savedAddresses.Add(new SavedAddress(label, address.Trim(), latitude, longitude));
    }

    public void RemoveSavedAddress(string label)
    {
        var address = _savedAddresses.FirstOrDefault(a => a.Label == label);
        if (address != null)
            _savedAddresses.Remove(address);
    }

    public void AddRating(decimal rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(rating));

        var totalScore = AverageRating * TotalRatings + rating;
        TotalRatings++;
        AverageRating = Math.Round(totalScore / TotalRatings, 2);
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
    public bool HasRatings => TotalRatings > 1;
}

/// <summary>
/// Saved address (owned by RiderProfile)
/// </summary>
public class SavedAddress
{
    public int Id { get; private set; }  // Auto-generated
    public string Label { get; private set; } = null!;  // e.g., "Home", "Work"
    public string Address { get; private set; } = null!;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private SavedAddress() { } // For EF Core

    public SavedAddress(string label, string address, double? latitude = null, double? longitude = null)
    {
        Label = label;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
    }
}
