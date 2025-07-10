using BenhaScooters.Domain;
using BenhaScooters.Domain.Driver;
using BenhaScooters.Domain.Driver.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations;

public class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
{
    public void Configure(EntityTypeBuilder<DriverProfile> builder)
    {
        builder.HasKey(x => x.Id);
        
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

            documents.Property(d => d.ProfileImageUrl)
                .HasMaxLength(500)
                .IsRequired();
        });

        // Configure DriverRating as owned entity
        builder.OwnsOne(x => x.Rating, rating =>
        {
            rating.Property(r => r.Rating)
                .HasPrecision(3, 2)
                .IsRequired();

            rating.Property(r => r.TotalRatings)
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

        builder.Property(x => x.IsOnline)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.OnboardingStatus);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.IsOnline);
    }
}
