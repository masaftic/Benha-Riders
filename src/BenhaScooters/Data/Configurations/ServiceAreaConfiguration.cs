using BenhaScooters.Domain.ServiceAreas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations;

public class ServiceAreaConfiguration : IEntityTypeConfiguration<ServiceArea>
{
    public void Configure(EntityTypeBuilder<ServiceArea> builder)
    {
        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sa => sa.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sa => sa.Area)
            .IsRequired()
            .HasColumnType("geometry(MultiPolygon, 4326)");
        
        builder.HasIndex(sa => sa.ExternalId)
            .IsUnique();
    }
}
