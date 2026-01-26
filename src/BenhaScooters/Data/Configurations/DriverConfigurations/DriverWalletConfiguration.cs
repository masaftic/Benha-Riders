using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BenhaScooters.Data.Configurations.DriverConfigurations;

public class DriverWalletConfiguration : IEntityTypeConfiguration<DriverWallet>
{
    public void Configure(EntityTypeBuilder<DriverWallet> builder)
    {
        builder.ToTable("DriverWallets");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.HasOne(x => x.Driver)
            .WithOne()
            .HasForeignKey<DriverWallet>(x => x.DriverUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DriverUserId)
            .IsUnique();

        builder.Property(x => x.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Configure transactions as owned collection
        builder.OwnsMany(x => x.Transactions, transaction =>
        {
            transaction.ToTable("WalletTransactions");
            
            transaction.WithOwner()
                .HasForeignKey(t => t.WalletId);

            transaction.HasKey(t => t.Id);
            
            transaction.Property(t => t.Id)
                .ValueGeneratedOnAdd();

            transaction.Property(t => t.Type)
                .HasConversion<string>()
                .IsRequired();

            transaction.Property(t => t.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            transaction.Property(t => t.BalanceAfter)
                .HasPrecision(18, 2)
                .IsRequired();

            transaction.Property(t => t.Description)
                .HasMaxLength(500)
                .IsRequired();

            transaction.Property(t => t.CreatedAt)
                .IsRequired();

            transaction.HasIndex(t => t.TripId);
            transaction.HasIndex(t => t.CreatedAt);
        });
    }
}
