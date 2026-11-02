using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Operations.Configurations;

public sealed class InstructorCourseRateConfiguration : IEntityTypeConfiguration<InstructorCourseRate>
{
    public void Configure(EntityTypeBuilder<InstructorCourseRate> builder)
    {
        builder.ToTable("InstructorCourseRates", table =>
            table.HasCheckConstraint("CK_InstructorCourseRates_Amount", "[Amount] > 0"));
        builder.HasKey(rate => rate.Id);
        builder.Property(rate => rate.Amount).HasPrecision(12, 2);
        builder.Property(rate => rate.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.HasIndex(rate => new { rate.InstructorUserId, rate.CourseOfferingId }).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(rate => rate.InstructorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(rate => rate.SetByManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rate => rate.CourseOffering).WithMany().HasForeignKey(rate => rate.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
