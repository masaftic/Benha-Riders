using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.UserConfigurations;

public class OtpSecurityEventConfiguration : IEntityTypeConfiguration<OtpSecurityEvent>
{
    public void Configure(EntityTypeBuilder<OtpSecurityEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Metadata)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.UserId, x.EventType, x.CreatedAt });
        builder.HasIndex(x => new { x.IpAddress, x.EventType, x.CreatedAt });
        builder.HasIndex(x => new { x.PhoneNumber, x.EventType, x.CreatedAt });
    }
}
