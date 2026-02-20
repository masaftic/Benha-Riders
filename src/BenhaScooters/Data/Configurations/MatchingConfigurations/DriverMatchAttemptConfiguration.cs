using BenhaScooters.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thinktecture;

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
        
        // Distance and Duration value objects are handled by Thinktecture converters
        builder.AddThinktectureValueConverters();

        builder.HasOne(dma => dma.DriverProfile)
            .WithMany()
            .HasForeignKey(dma => dma.DriverUserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(dma => dma.Status);
        builder.HasIndex(dma => dma.CreatedAt);
    }
}
