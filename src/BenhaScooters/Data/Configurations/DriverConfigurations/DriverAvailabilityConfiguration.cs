using BenhaScooters.Domain.Drivers;
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
            .HasConversion<int>();

        builder.HasOne(da => da.Driver)
            .WithOne()
            .HasForeignKey<DriverAvailability>(da => da.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(da => da.Status);
        builder.HasIndex(da => da.LastLocationUpdate);
    }
}
