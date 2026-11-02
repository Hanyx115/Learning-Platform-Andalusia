using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Operations.Configurations;

public sealed class CompensationPayoutConfiguration : IEntityTypeConfiguration<CompensationPayout>
{
    public void Configure(EntityTypeBuilder<CompensationPayout> builder)
    {
        builder.ToTable("CompensationPayouts", table =>
        {
            table.HasCheckConstraint("CK_CompensationPayouts_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_CompensationPayouts_Type", "[PayoutType] IN ('InstructorCourse', 'StaffMonthly')");
            table.HasCheckConstraint("CK_CompensationPayouts_Status", "[Status] IN ('Draft', 'Reviewed', 'Paid')");
        });
        builder.HasKey(payout => payout.Id);
        builder.Property(payout => payout.Amount).HasPrecision(12, 2);
        builder.Property(payout => payout.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(payout => payout.PayoutType).HasMaxLength(32).IsRequired();
        builder.Property(payout => payout.Status).HasMaxLength(24).IsRequired();
        builder.Property(payout => payout.Reason).HasMaxLength(300).IsRequired();
        builder.HasIndex(payout => payout.InstructorCourseRateId)
            .HasFilter("[InstructorCourseRateId] IS NOT NULL").IsUnique();
        builder.HasIndex(payout => new { payout.UserId, payout.PeriodMonth, payout.PayoutType })
            .HasFilter("[PeriodMonth] IS NOT NULL").IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(payout => payout.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(payout => payout.ReviewedByManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(payout => payout.ApprovedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(payout => payout.InstructorCourseRate).WithMany()
            .HasForeignKey(payout => payout.InstructorCourseRateId).OnDelete(DeleteBehavior.Restrict);
    }
}
