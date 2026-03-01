using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.UserConfigurations;

public class SmsLogConfiguration : IEntityTypeConfiguration<SmsLog>
{
    public void Configure(EntityTypeBuilder<SmsLog> builder)
    {
        builder.ToTable("sms_logs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired(false);

        builder.Property(s => s.PhoneNumber)
            .HasMaxLength(20)
            .HasColumnName("phone_number")
            .IsRequired();

        builder.Property(s => s.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(s => s.Message)
            .HasMaxLength(500)
            .HasColumnName("message")
            .IsRequired();

        builder.Property(s => s.ProviderId)
            .HasMaxLength(100)
            .HasColumnName("provider_id")
            .IsRequired(false);

        builder.Property(s => s.ProviderUid)
            .HasMaxLength(100)
            .HasColumnName("provider_uid")
            .IsRequired(false);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(s => s.Cost)
            .HasColumnName("cost")
            .HasDefaultValue(0);

        builder.Property(s => s.SmsCount)
            .HasColumnName("sms_count")
            .HasDefaultValue(1);

        builder.Property(s => s.ErrorMessage)
            .HasMaxLength(1000)
            .HasColumnName("error_message")
            .IsRequired(false);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45)
            .HasColumnName("ip_address")
            .IsRequired(false);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.DeliveredAt)
            .HasColumnName("delivered_at")
            .IsRequired(false);

        // Foreign key relationship
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for querying
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_sms_logs_user_id");

        builder.HasIndex(s => s.PhoneNumber)
            .HasDatabaseName("ix_sms_logs_phone_number");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_sms_logs_status");

        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("ix_sms_logs_created_at");

        builder.HasIndex(s => s.ProviderId)
            .HasDatabaseName("ix_sms_logs_provider_id");
    }
}
