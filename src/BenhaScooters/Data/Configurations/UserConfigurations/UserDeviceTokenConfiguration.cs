using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.UserConfigurations;

public class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.ToTable("UserDeviceTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.Token).IsRequired().HasMaxLength(512);
        builder.Property(t => t.Platform).IsRequired().HasMaxLength(20);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.LastUsedAt).IsRequired();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one token per user per platform
        builder.HasIndex(t => new { t.UserId, t.Platform }).IsUnique();
        builder.HasIndex(t => t.Token);
    }
}
