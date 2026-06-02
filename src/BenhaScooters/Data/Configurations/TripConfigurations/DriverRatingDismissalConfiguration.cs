using BenhaScooters.Domain.Ratings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class DriverRatingDismissalConfiguration : IEntityTypeConfiguration<DriverRatingDismissal>
{
    public void Configure(EntityTypeBuilder<DriverRatingDismissal> builder)
    {
        builder.ToTable("DriverRatingDismissals");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.DismissedAt).IsRequired();

        builder.HasOne(d => d.Trip)
            .WithMany()
            .HasForeignKey(d => d.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.TripId, d.RiderId }).IsUnique();
        builder.HasIndex(d => d.RiderId);
    }
}
