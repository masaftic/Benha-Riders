using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverStatsConfiguration : IEntityTypeConfiguration<DriverStats>
{
    public void Configure(EntityTypeBuilder<DriverStats> builder)
    {
        builder.ToTable("DriverStats");
        
        // UserId is PK (1:1 with User)
        builder.HasKey(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<DriverStats>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.AverageRating)
            .HasPrecision(3, 2)  // e.g., 4.85
            .IsRequired();

        builder.Property(x => x.TotalRatings)
            .IsRequired();

        builder.Property(x => x.TotalTrips)
            .IsRequired();

        builder.Property(x => x.CompletedTrips)
            .IsRequired();

        builder.Property(x => x.CancelledTrips)
            .IsRequired();

        builder.Property(x => x.CurrentStreak)
            .IsRequired();

        builder.Property(x => x.TotalOnlineTime)
            .IsRequired();

        // Index for leaderboards/queries by rating
        builder.HasIndex(x => x.AverageRating);
        builder.HasIndex(x => x.CompletedTrips);
    }
}
