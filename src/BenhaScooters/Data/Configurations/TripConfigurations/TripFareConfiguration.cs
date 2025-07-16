using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.TripConfigurations;

public class TripFareConfiguration : IEntityTypeConfiguration<TripFare>
{
    public void Configure(EntityTypeBuilder<TripFare> builder)
    {
        builder.ToTable("TripFares");

        builder.HasKey(tf => tf.Id);
        builder.Property(tf => tf.Id)
            .ValueGeneratedOnAdd();

        builder.Property(tf => tf.TripId)
            .IsRequired();

        builder.Property(tf => tf.BaseFare)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(tf => tf.DistanceFare)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(tf => tf.DurationFare)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(tf => tf.TotalFare)
            .HasColumnName("TotalFare")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne<Trip>()
            .WithOne(t => t.TripFare)
            .HasForeignKey<TripFare>(tf => tf.TripId);
    }
}
