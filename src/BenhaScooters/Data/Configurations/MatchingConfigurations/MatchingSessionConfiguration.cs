using BenhaScooters.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.MatchingConfigurations;

public class MatchingSessionConfiguration : IEntityTypeConfiguration<MatchingSession>
{
    public void Configure(EntityTypeBuilder<MatchingSession> builder)
    {
        builder.HasKey(ms => ms.Id);
        builder.Property(ms => ms.Id).ValueGeneratedOnAdd();

        builder.Property(ms => ms.CurrentMode)
            .HasConversion<string>();

        builder.Property(ms => ms.Status)
            .HasConversion<string>();

        builder.HasOne(ms => ms.TripRequest)
            .WithMany()
            .HasForeignKey(ms => ms.TripRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ms => ms.MatchAttempts)
            .WithOne(ma => ma.MatchingSession)
            .HasForeignKey(ma => ma.MatchingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ms => ms.Status);
        builder.HasIndex(ms => ms.CreatedAt);
        builder.HasIndex(ms => ms.TripRequestId).IsUnique();
    }
}
