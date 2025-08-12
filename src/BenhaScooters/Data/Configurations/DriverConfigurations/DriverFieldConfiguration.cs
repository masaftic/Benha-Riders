using BenhaScooters.Domain.Drivers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverFieldConfiguration : IEntityTypeConfiguration<DriverField>
{
    public void Configure(EntityTypeBuilder<DriverField> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();

        builder.Property(f => f.FieldName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Status)
            .IsRequired();
        
        builder.Property(f => f.RejectionReason)
            .HasMaxLength(500);

        builder.Property(f => f.Step)
            .IsRequired();

        builder.HasOne(f => f.Driver)
            .WithMany(d => d.Fields)
            .HasForeignKey(f => f.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
