using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverAvailabilityConfiguration : IEntityTypeConfiguration<DriverAvailability>
{
    public void Configure(EntityTypeBuilder<DriverAvailability> builder)
    {
        builder.HasKey(da => da.Id);
        builder.Property(da => da.Id).ValueGeneratedOnAdd();
        
        builder.Property(da => da.Status)
            .HasConversion<string>();

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(da => da.CurrentTripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(da => da.Status);
    }
}
