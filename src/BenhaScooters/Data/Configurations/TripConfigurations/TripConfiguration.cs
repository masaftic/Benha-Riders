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

        builder.Property(t => t.AssignedAt)
            .IsRequired();

        builder.Property(t => t.DriverArrivedAt);

        builder.Property(t => t.StartedAt);

        builder.Property(t => t.CompletedAt);


        builder.OwnsOne(t => t.EstimatedFare, ef =>
        {
            ef.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        builder.OwnsOne(t => t.TripPayment, tp =>
        {
            tp.Property(p => p.Method)
                .HasConversion<string>()
                .IsRequired();

            tp.Property(p => p.Status)
                .HasConversion<string>()
                .IsRequired();

            tp.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            tp.Property(p => p.PaidAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            tp.Property(p => p.ExternalReference)
                .HasMaxLength(100);
        });

        builder.HasOne(t => t.Driver)
            .WithMany()
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Rider)
            .WithMany()
            .HasForeignKey(t => t.RiderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Status);
    }
}
