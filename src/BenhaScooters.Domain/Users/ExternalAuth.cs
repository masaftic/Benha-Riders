namespace BenhaScooters.Domain.Users;


public class ExternalAuth
{
    public string Provider { get; private set; } = null!;
    public string ProviderUserId { get; private set; } = null!;

    public UserId UserId { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private ExternalAuth() { }

    public ExternalAuth(string provider, string providerUserId)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be null or empty.", nameof(provider));

        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new ArgumentException("Provider user ID cannot be null or empty.", nameof(providerUserId));

        Provider = provider;
        ProviderUserId = providerUserId;
    }
}
