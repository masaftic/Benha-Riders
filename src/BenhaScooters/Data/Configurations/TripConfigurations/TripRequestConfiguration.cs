using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class TripRequestConfiguration : IEntityTypeConfiguration<TripRequest>
{
    public void Configure(EntityTypeBuilder<TripRequest> builder)
    {
        builder.HasKey(tr => tr.Id);
        builder.Property(tr => tr.Id).ValueGeneratedOnAdd();
        
        builder.Property(tr => tr.PickupLocation)
            .HasColumnType("geography (point)");
            
        builder.Property(tr => tr.DropoffLocation)
            .HasColumnType("geography (point)");
            
        builder.Property(tr => tr.PickupAddress)
            .HasMaxLength(500);
            
        builder.Property(tr => tr.DropoffAddress)
            .HasMaxLength(500);
            
        builder.Property(tr => tr.Status)
            .HasConversion<string>();
            
        builder.Property(tr => tr.CancellationReason)
            .HasMaxLength(500);

        builder.OwnsOne(tr => tr.EstimatedFare);

        builder.HasOne(tr => tr.Rider)
            .WithMany()
            .HasForeignKey(tr => tr.RiderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(tr => tr.MatchedDriver)
            .WithMany()
            .HasForeignKey(tr => tr.MatchedDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(tr => tr.Status);
        builder.HasIndex(tr => tr.RequestedAt);
    }
}
