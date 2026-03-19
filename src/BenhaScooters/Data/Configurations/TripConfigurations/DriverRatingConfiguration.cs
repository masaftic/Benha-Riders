using BenhaScooters.Domain.Ratings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class DriverRatingConfiguration : IEntityTypeConfiguration<DriverRating>
{
    public void Configure(EntityTypeBuilder<DriverRating> builder)
    {
        builder.ToTable("DriverRatings");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(500);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasOne(r => r.Trip)
            .WithMany()
            .HasForeignKey(r => r.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // One rating per trip per rider
        builder.HasIndex(r => new { r.TripId, r.RiderId }).IsUnique();
        builder.HasIndex(r => r.DriverId);
    }
}
