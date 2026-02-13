using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thinktecture;

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

        builder.OwnsOne(tr => tr.FinalFare, ef =>
        {
            ef.Property(e => e.Amount)
                .HasColumnName("FinalFare_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            ef.Property(e => e.Distance)
                .HasColumnName("FinalFare_Distance");
            ef.Property(e => e.Time)
                .HasColumnName("FinalFare_Time");
            
            ef.AddThinktectureValueConverters();
        });

        // RiderId/DriverId are semantic wrappers around UserId
        builder.HasOne(tr => tr.RiderProfile)
            .WithMany(rp => rp.TripRequests)
            .HasForeignKey(tr => tr.RiderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(tr => tr.MatchedDriverProfile)
            .WithMany()
            .HasForeignKey(tr => tr.MatchedDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(tr => tr.Status);
        builder.HasIndex(tr => tr.RequestedAt);
    }
}
