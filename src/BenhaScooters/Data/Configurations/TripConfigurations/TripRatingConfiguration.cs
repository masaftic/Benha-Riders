using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class TripRatingConfiguration : IEntityTypeConfiguration<TripRating>
{
    public void Configure(EntityTypeBuilder<TripRating> builder)
    {
        builder.ToTable("TripRatings");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.DriverComment).HasMaxLength(500);
        builder.Property(r => r.RiderComment).HasMaxLength(500);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasOne(r => r.Trip)
            .WithOne()
            .HasForeignKey<TripRating>(r => r.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.TripId).IsUnique();
        builder.HasIndex(r => r.DriverId);
        builder.HasIndex(r => r.RiderId);
    }
}
