using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.PickupLocation)
            .HasColumnType("geography (point)");
            
        builder.Property(t => t.DropoffLocation)
            .HasColumnType("geography (point)");
            
        builder.Property(t => t.PickupAddress)
            .HasMaxLength(500);
            
        builder.Property(t => t.DropoffAddress)
            .HasMaxLength(500);
            
        builder.Property(t => t.Status)
            .HasConversion<int>();
            
        builder.OwnsOne(t => t.EstimatedFare);

        builder.HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Rider)
            .WithMany()
            .HasForeignKey(t => t.RiderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(t => t.TripRoute)
            .WithOne()
            .HasForeignKey<TripRoute>(tr => tr.TripId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne<TripRequest>()
            .WithOne()
            .HasForeignKey<Trip>(t => t.TripRequestId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // builder.HasOne(t => t.Rating)
        //     .WithOne()
        //     .HasForeignKey<TripRating>(tr => tr.TripId)
        //     .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedAt);
    }
}
