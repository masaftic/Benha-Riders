using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverRatingConfiguration : IEntityTypeConfiguration<DriverRating>
{
    public void Configure(EntityTypeBuilder<DriverRating> builder)
    {
        builder.HasKey(r => r.DriverId);

        builder.Property(r => r.AverageRating)
            .HasPrecision(3, 2);

        builder.Property(r => r.TotalRatings)
            .IsRequired();

        builder.HasOne(r => r.Driver)
            .WithOne()
            .HasForeignKey<DriverRating>(r => r.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
