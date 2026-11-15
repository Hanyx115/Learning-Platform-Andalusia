using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Operations.Configurations;

public sealed class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
{
    public void Configure(EntityTypeBuilder<WalletAccount> builder)
    {
        builder.ToTable("WalletAccounts", table =>
            table.HasCheckConstraint("CK_WalletAccounts_Balance", "[Balance] >= 0"));
        builder.HasKey(account => account.UserId);
        builder.Property(account => account.Balance).HasPrecision(12, 2);
        builder.Property(account => account.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<WalletAccount>(account => account.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
