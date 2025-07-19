using BenhaScooters.Domain.Riders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.UserConfigurations;

public class RiderConfiguration : IEntityTypeConfiguration<Rider>
{
    public void Configure(EntityTypeBuilder<Rider> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.PreferredName)
            .HasMaxLength(100);

        builder.Property(r => r.DefaultPaymentMethodId)
            .HasMaxLength(50);

        // builder.OwnsOne(r => r.Preferences);
        builder.OwnsOne(r => r.Rating);

        builder.Property(r => r.SavedAddresses)
            .HasConversion(
                v => string.Join(';', v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

        builder.HasOne(r => r.User)
            .WithOne()
            .HasForeignKey<Rider>(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
