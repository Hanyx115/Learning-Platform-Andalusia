using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Operations.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Amount).HasPrecision(12, 2);
        builder.Property(transaction => transaction.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(transaction => transaction.Type).HasMaxLength(40).IsRequired();
        builder.Property(transaction => transaction.Reference).HasMaxLength(120).IsRequired();
        builder.HasIndex(transaction => transaction.Reference).IsUnique();
        builder.HasIndex(transaction => new { transaction.UserId, transaction.CreatedAtUtc });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
