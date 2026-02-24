using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
{
    public void Configure(EntityTypeBuilder<DriverProfile> builder)
    {
        builder.ToTable("DriverProfiles");
        
        // UserId is both PK and FK to User
        builder.HasKey(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithOne(u => u.DriverProfile)
            .HasForeignKey<DriverProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.OnboardingStatus)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Configure PersonalInfo as owned entity
        builder.OwnsOne(x => x.PersonalInfo, personalInfo =>
        {
            personalInfo.Property(p => p.FullName)
                .HasColumnName("PersonalInfo_FullName")
                .HasMaxLength(100)
                .IsRequired();

            personalInfo.Property(p => p.NationalId)
                .HasColumnName("PersonalInfo_NationalId")
                .IsRequired();
        });

        // Configure Vehicle as owned entity
        builder.OwnsOne(x => x.Vehicle, vehicle =>
        {
            vehicle.Property(v => v.VehicleType)
                .HasColumnName("Vehicle_Type")
                .HasConversion<string>()
                .IsRequired();

            vehicle.Property(v => v.Brand)
                .HasColumnName("Vehicle_Brand")
                .HasMaxLength(50)
                .IsRequired();

            vehicle.Property(v => v.Model)
                .HasColumnName("Vehicle_Model")
                .HasMaxLength(50)
                .IsRequired();

            vehicle.Property(v => v.Color)
                .HasColumnName("Vehicle_Color")
                .HasMaxLength(30)
                .IsRequired();

            vehicle.Property(v => v.LicensePlate)
                .HasColumnName("Vehicle_LicensePlate")
                .IsRequired();

            vehicle.Property(v => v.Year)
                .HasColumnName("Vehicle_Year")
                .IsRequired();
        });

        // Configure Documents as owned collection
        builder.OwnsMany(x => x.Documents, document =>
        {
            document.ToTable("DriverDocuments");
            
            document.WithOwner().HasForeignKey(d => d.DriverUserId);
            
            document.HasKey(d => d.Id);
            document.Property(d => d.Id)
                .ValueGeneratedOnAdd();

            document.Property(d => d.Type)
                .HasConversion<string>()
                .IsRequired();

            document.Property(d => d.ImageUrl)
                .HasMaxLength(500)
                .IsRequired();

            document.Property(d => d.UploadedAt)
                .IsRequired();

            document.HasIndex(d => new { d.DriverUserId, d.Type })
                .IsUnique();
        });

        // Indexes
        builder.HasIndex(x => x.OnboardingStatus);
    }
}
