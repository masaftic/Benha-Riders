using BenhaScooters.Domain.Drivers;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverLocationConfiguration : IEntityTypeConfiguration<DriverLocation>
{
    public void Configure(EntityTypeBuilder<DriverLocation> builder)
    {
        builder.HasKey(dl => dl.DriverId);

        builder.Property(dl => dl.Location)
            .HasColumnType("geography (point)");

        builder.HasOne(dl => dl.Driver)
            .WithOne()
            .HasForeignKey<DriverLocation>(dl => dl.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dl => dl.Location).HasMethod("GIST");
        builder.HasIndex(dl => dl.Timestamp);
    }
}
