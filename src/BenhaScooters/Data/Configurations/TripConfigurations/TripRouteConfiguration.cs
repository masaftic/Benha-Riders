using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class TripRouteConfiguration : IEntityTypeConfiguration<TripRoute>
{
    public void Configure(EntityTypeBuilder<TripRoute> builder)
    {
        builder.ToTable("TripRoutes");
        builder.HasKey(tr => tr.TripId);

        builder.Property(tr => tr.TripId)
            .IsRequired();

        builder.Property(tr => tr.Path)
            .IsRequired()
            .HasColumnType("geometry (LineString, 4326)");

        builder.HasOne(r => r.Trip)
            .WithOne()
            .HasForeignKey<TripRoute>(r => r.TripId);

        builder.HasMany(tr => tr.TripGpsPoints)
            .WithOne(tgp => tgp.TripRoute)
            .HasForeignKey(gp => gp.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}