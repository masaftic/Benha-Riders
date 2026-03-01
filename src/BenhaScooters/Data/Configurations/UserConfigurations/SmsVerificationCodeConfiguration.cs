using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.UserConfigurations;

public class SmsVerificationCodeConfiguration : IEntityTypeConfiguration<SmsVerificationCode>
{
    public void Configure(EntityTypeBuilder<SmsVerificationCode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.CodeHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .IsRequired();

        builder.Property(x => x.FailedAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => new { x.UserId, x.PhoneNumber, x.CreatedAt });
        builder.HasIndex(x => new { x.UserId, x.IsUsed, x.ExpiresAt }); // For finding active codes
    }
}
