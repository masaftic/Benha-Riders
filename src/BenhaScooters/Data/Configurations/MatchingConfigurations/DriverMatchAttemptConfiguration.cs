using BenhaScooters.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.MatchingConfigurations;

public class DriverMatchAttemptConfiguration : IEntityTypeConfiguration<DriverMatchAttempt>
{
    public void Configure(EntityTypeBuilder<DriverMatchAttempt> builder)
    {
        builder.HasKey(dma => dma.Id);
        builder.Property(dma => dma.Id).ValueGeneratedOnAdd();

        builder.Property(dma => dma.Status)
            .HasConversion<int>();
            
        builder.Property(dma => dma.RejectionReason)
            .HasMaxLength(500);
            
        builder.Property(dma => dma.DriverScore)
            .HasPrecision(5, 2);
            
        builder.Property(dma => dma.DistanceToPickup)
            .HasPrecision(8, 3);
            
        builder.Property(dma => dma.EstimatedArrivalTime)
            .HasPrecision(5, 2);

        builder.HasOne(dma => dma.Driver)
            .WithMany()
            .HasForeignKey(dma => dma.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(dma => dma.TripRequest)
            .WithMany()
            .HasForeignKey(dma => dma.TripRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dma => dma.Status);
        builder.HasIndex(dma => dma.CreatedAt);
    }
}
