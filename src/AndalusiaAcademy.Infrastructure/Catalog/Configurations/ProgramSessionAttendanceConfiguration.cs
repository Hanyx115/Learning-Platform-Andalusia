using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class ProgramSessionAttendanceConfiguration : IEntityTypeConfiguration<ProgramSessionAttendance>
{
    public void Configure(EntityTypeBuilder<ProgramSessionAttendance> builder)
    {
        builder.ToTable("ProgramSessionAttendances", table =>
        {
            table.HasCheckConstraint(
                "CK_ProgramSessionAttendances_Status",
                "[Status] IN ('Present', 'Absent', 'Late', 'Excused')");
        });

        builder.HasKey(attendance => attendance.Id);
        builder.Property(attendance => attendance.Status).HasMaxLength(32).IsRequired();
        builder.Property(attendance => attendance.Note).HasMaxLength(500);

        builder.HasIndex(attendance => new { attendance.ProgramSessionId, attendance.UserId }).IsUnique();

        builder.HasOne(attendance => attendance.ProgramSession)
            .WithMany()
            .HasForeignKey(attendance => attendance.ProgramSessionId)
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
