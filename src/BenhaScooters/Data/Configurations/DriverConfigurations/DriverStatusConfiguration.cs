using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverStatusConfiguration : IEntityTypeConfiguration<DriverStatus>
{
    public void Configure(EntityTypeBuilder<DriverStatus> builder)
    {
        builder.ToTable("DriverStatuses");
        
        // UserId is PK (1:1 with User)
        builder.HasKey(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<DriverStatus>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.LastStatusChange)
            .IsRequired();

        // Index for querying available drivers
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.Status, x.UserId });
    }
}
