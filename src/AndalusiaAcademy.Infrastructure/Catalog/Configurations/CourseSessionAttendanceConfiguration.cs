using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class CourseSessionAttendanceConfiguration : IEntityTypeConfiguration<CourseSessionAttendance>
{
    public void Configure(EntityTypeBuilder<CourseSessionAttendance> builder)
    {
        builder.ToTable("CourseSessionAttendances", table =>
        {
            table.HasCheckConstraint(
                "CK_CourseSessionAttendances_Status",
                "[Status] IN ('Present', 'Absent', 'Late', 'Excused')");
        });

        builder.HasKey(attendance => attendance.Id);
        builder.Property(attendance => attendance.Status).HasMaxLength(32).IsRequired();
        builder.Property(attendance => attendance.Note).HasMaxLength(500);

        builder.HasIndex(attendance => new { attendance.CourseSessionId, attendance.UserId }).IsUnique();

        builder.HasOne(attendance => attendance.CourseSession)
            .WithMany()
            .HasForeignKey(attendance => attendance.CourseSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(attendance => attendance.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(attendance => attendance.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
