using BenhaScooters.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        
        builder.HasKey(rt => rt.Id);
        
        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();
            
        builder.Property(rt => rt.CreatedAt)
            .IsRequired();
            
        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(rt => rt.RevokedAt)
            .IsRequired(false);

        builder.HasIndex(rt => rt.Token)
            .IsUnique();
            
        builder.HasIndex(rt => rt.UserId);
        
        builder.HasIndex(rt => rt.ExpiresAt);
    }
}
