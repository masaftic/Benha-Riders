using BenhaScooters.Domain.Riders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.UserConfigurations;

public class RiderProfileConfiguration : IEntityTypeConfiguration<RiderProfile>
{
    public void Configure(EntityTypeBuilder<RiderProfile> builder)
    {
        builder.ToTable("RiderProfiles");
        
        // UserId is PK (1:1 with User)
        builder.HasKey(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<RiderProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.PreferredName)
            .HasMaxLength(100);

        builder.Property(x => x.DefaultPaymentMethodId)
            .HasMaxLength(100);

        builder.Property(x => x.AverageRating)
            .HasPrecision(3, 2)
            .IsRequired();

        builder.Property(x => x.TotalRatings)
            .IsRequired();

        builder.Property(x => x.TotalTrips)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Configure SavedAddresses as owned collection
        builder.OwnsMany(x => x.SavedAddresses, address =>
        {
            address.ToTable("RiderSavedAddresses");
            
            address.WithOwner().HasForeignKey("RiderUserId");
            
            address.HasKey(a => a.Id);
            address.Property(a => a.Id)
                .ValueGeneratedOnAdd();

            address.Property(a => a.Label)
                .HasMaxLength(50)
                .IsRequired();

            address.Property(a => a.Address)
                .HasMaxLength(500)
                .IsRequired();

            address.HasIndex("RiderUserId", "Label")
                .IsUnique();
        });
    }
}
