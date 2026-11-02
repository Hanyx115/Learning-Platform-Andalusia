using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Operations.Configurations;

public sealed class StaffCompensationRateConfiguration : IEntityTypeConfiguration<StaffCompensationRate>
{
    public void Configure(EntityTypeBuilder<StaffCompensationRate> builder)
    {
        builder.ToTable("StaffCompensationRates", table =>
            table.HasCheckConstraint("CK_StaffCompensationRates_Amount", "[MonthlyAmount] > 0"));
        builder.HasKey(rate => rate.UserId);
        builder.Property(rate => rate.MonthlyAmount).HasPrecision(12, 2);
        builder.Property(rate => rate.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<StaffCompensationRate>(rate => rate.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(rate => rate.SetByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
