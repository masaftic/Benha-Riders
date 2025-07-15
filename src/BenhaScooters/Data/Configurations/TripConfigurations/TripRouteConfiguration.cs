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

        builder.HasKey(tr => tr.Id);

        builder.Property(tr => tr.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(tr => tr.TripId)
            .IsRequired();

        builder.Property(tr => tr.Path)
            .IsRequired()
            .HasColumnType("geometry (LineString, 4326)");

        builder.HasOne(tr => tr.Trip)
            .WithOne(t => t.TripRoute)
            .HasForeignKey<TripRoute>(tr => tr.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}