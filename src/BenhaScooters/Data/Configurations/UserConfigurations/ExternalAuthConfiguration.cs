using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.UserConfigurations;

public class ExternalAuthConfiguration : IEntityTypeConfiguration<ExternalAuth>
{
    public void Configure(EntityTypeBuilder<ExternalAuth> builder)
    {
        builder.ToTable("ExternalAuths");

        builder.HasKey(ea => new { ea.Provider, ea.ProviderUserId });

        builder.Property(ea => ea.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ea => ea.ProviderUserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(ea => ea.User)
            .WithMany(u => u.ExternalAuths)
            .HasForeignKey(ea => ea.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}