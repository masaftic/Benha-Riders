using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class LocationPingConfiguration : IEntityTypeConfiguration<TripGpsPoint>
{
    public void Configure(EntityTypeBuilder<TripGpsPoint> builder)
    {
        builder.HasKey(lp => lp.Id);
        builder.Property(lp => lp.Id).ValueGeneratedOnAdd();

        builder.Property(lp => lp.Location)
            .HasColumnType("geography (point)");

        builder.Property(lp => lp.Heading)
            .HasPrecision(5, 2);
            
        builder.Property(lp => lp.Speed)
            .HasPrecision(5, 2);

        builder.HasOne(lp => lp.Driver)
            .WithMany()
            .HasForeignKey(lp => lp.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(lp => lp.Trip)
            .WithMany()
            .HasForeignKey(lp => lp.TripId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(lp => lp.Timestamp);
        builder.HasIndex(lp => lp.Location).HasMethod("GIST");
    }
}
