using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverLocationConfiguration : IEntityTypeConfiguration<DriverLocation>
{
    public void Configure(EntityTypeBuilder<DriverLocation> builder)
    {
        builder.ToTable("DriverLocations");
        
        // UserId is PK (1:1 with User)
        builder.HasKey(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<DriverLocation>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Location)
            .HasColumnType("geography (point)")
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        // Spatial index for proximity queries
        builder.HasIndex(x => x.Location).HasMethod("GIST");
        builder.HasIndex(x => x.Timestamp);
    }
}
