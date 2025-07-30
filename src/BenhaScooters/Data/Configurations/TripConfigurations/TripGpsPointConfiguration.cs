using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class TripGpsPointConfiguration : IEntityTypeConfiguration<TripGpsPoint>
{
    public void Configure(EntityTypeBuilder<TripGpsPoint> builder)
    {
        builder.HasKey(lp => lp.Id);
        builder.Property(lp => lp.Id).ValueGeneratedOnAdd();

        builder.Property(lp => lp.Location)
            .HasColumnType("geography (point)");

        builder.HasOne(lp => lp.TripRoute)
            .WithMany(tr => tr.TripGpsPoints)
            .HasForeignKey(lp => lp.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lp => lp.Timestamp);
        builder.HasIndex(lp => lp.Location).HasMethod("GIST");
    }
}
