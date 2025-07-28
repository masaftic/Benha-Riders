using BenhaScooters.Domain.Drivers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverDocumentConfiguration : IEntityTypeConfiguration<DriverDocument>
{
    public void Configure(EntityTypeBuilder<DriverDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Type)
            .IsRequired();

        builder.Property(d => d.Status)
            .IsRequired();

        builder.Property(d => d.UploadedAt)
            .IsRequired();

        builder.HasOne(d => d.Driver)
            .WithMany(dr => dr.Documents)
            .HasForeignKey(d => d.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}