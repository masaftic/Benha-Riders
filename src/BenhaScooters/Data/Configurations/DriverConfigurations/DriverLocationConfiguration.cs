using BenhaScooters.Domain.Drivers;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverLocationConfiguration : IEntityTypeConfiguration<DriverLocation>
{
    public void Configure(EntityTypeBuilder<DriverLocation> builder)
    {
        builder.HasKey(dl => dl.Id);
        builder.Property(dl => dl.Id).ValueGeneratedOnAdd();

        builder.Property(dl => dl.Location)
            .HasColumnType("geography (point)");

        builder.Property(dl => dl.Heading)
            .HasPrecision(5, 2);

        builder.Property(dl => dl.Speed)
            .HasPrecision(5, 2);


        builder.HasIndex(dl => dl.Location).HasMethod("GIST");
        builder.HasIndex(dl => dl.Timestamp);
    }
}
