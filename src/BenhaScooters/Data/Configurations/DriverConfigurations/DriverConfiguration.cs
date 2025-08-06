using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
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

        builder.HasOne(d => d.Vehicle)
            .WithOne(v => v.Driver)
            .HasForeignKey<DriverVehicle>(v => v.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Info as owned entity
        builder.OwnsOne(x => x.Info, personalInfo =>
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

        // Onboarding State
        builder.OwnsOne(x => x.OnboardingState, onboardingState =>
        {
            onboardingState.Property(os => os.Status)
                .HasConversion<string>()
                .IsRequired();

            onboardingState.Property(os => os.CurrentStep)
                .HasConversion<string>()
                .IsRequired();

            onboardingState.Property(os => os.RejectionReason)
                .HasMaxLength(500);

            onboardingState.Property(os => os.CreatedAt)
                .IsRequired();

            onboardingState.Property(os => os.CompletedAt);
        });

        // Driver Status
        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.IsActive);
    }
}
