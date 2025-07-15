using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .IsRequired();
        
        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Driver>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Configure PersonalInfo as owned entity
        builder.OwnsOne(x => x.PersonalInfo, personalInfo =>
        {
            personalInfo.Property(p => p.FullName)
                .HasMaxLength(100)
                .IsRequired();

            personalInfo.Property(p => p.NationalId)
                .IsRequired();

            personalInfo.Property(p => p.DateOfBirth)
                .IsRequired();

            personalInfo.Property(p => p.Address)
                .HasMaxLength(500)
                .IsRequired();
            
            personalInfo.Property(p => p.City)
                .HasMaxLength(100)
                .IsRequired();

            personalInfo.Property(p => p.EmergencyContactName)
                .HasMaxLength(100)
                .IsRequired();

            personalInfo.Property(p => p.EmergencyContactPhone)
                .IsRequired();
            
            personalInfo.HasIndex(p => p.NationalId)
                .IsUnique();
        });

        // Configure VehicleInfo as owned entity
        builder.OwnsOne(x => x.VehicleInfo, vehicleInfo =>
        {
            vehicleInfo.Property(v => v.VehicleType)
                .HasConversion<string>()
                .IsRequired();

            vehicleInfo.Property(v => v.Brand)
                .HasMaxLength(50)
                .IsRequired();

            vehicleInfo.Property(v => v.Model)
                .HasMaxLength(50)
                .IsRequired();

            vehicleInfo.Property(v => v.Color)
                .HasMaxLength(30)
                .IsRequired();

            vehicleInfo.Property(v => v.LicensePlate)
                .HasMaxLength(30)
                .IsRequired();

            vehicleInfo.Property(v => v.Year)
                .IsRequired();
            
            vehicleInfo.HasIndex(v => v.LicensePlate)
                .IsUnique();
        });

        // Configure DriverDocuments as owned entity
        builder.OwnsOne(x => x.Documents, documents =>
        {
            documents.Property(d => d.LicenseImageUrl)
                .HasMaxLength(500)
                .IsRequired();

            documents.Property(d => d.VehicleRegistrationImageUrl)
                .HasMaxLength(500)
                .IsRequired();

            documents.Property(d => d.ImageUrl)
                .HasMaxLength(500)
                .IsRequired();
        });

        // Onboarding Status
        builder.Property(x => x.OnboardingStatus)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.CurrentStep)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        // Driver Status
        builder.Property(x => x.IsActive)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.OnboardingStatus);
        builder.HasIndex(x => x.IsActive);
    }
}
