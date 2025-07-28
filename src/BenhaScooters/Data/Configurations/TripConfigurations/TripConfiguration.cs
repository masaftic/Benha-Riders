using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.PickupLocation)
            .HasColumnType("geography (point)");

        builder.Property(t => t.DropoffLocation)
            .HasColumnType("geography (point)");

        builder.Property(t => t.PickupAddress)
            .HasMaxLength(500);

        builder.Property(t => t.DropoffAddress)
            .HasMaxLength(500);

        builder.OwnsOne(t => t.TripFare, fare =>
        {
            fare.Property(tf => tf.BaseFare)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            fare.Property(tf => tf.DistanceFare)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            fare.Property(tf => tf.DurationFare)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            fare.Property(tf => tf.TotalFare)
                .HasColumnName("TotalFare")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        builder.Property(t => t.Status)
            .HasConversion<string>();

        builder.OwnsMany(t => t.Events, e =>
        {
            e.Property(ev => ev.Status)
                .HasConversion<string>()
                .IsRequired();

            e.Property(ev => ev.Timestamp)
                .IsRequired();
        });

        builder.OwnsOne(t => t.EstimatedFare, ef =>
        {
            ef.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        builder.HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Rider)
            .WithMany()
            .HasForeignKey(t => t.RiderId)
            .OnDelete(DeleteBehavior.Cascade);


        // builder.HasOne(t => t.Rating)
        //     .WithOne()
        //     .HasForeignKey<TripRating>(tr => tr.TripId)
        //     .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Status);
    }
}
