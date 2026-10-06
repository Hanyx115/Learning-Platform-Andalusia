using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class ProgramCourseConfiguration : IEntityTypeConfiguration<ProgramCourse>
{
    public void Configure(EntityTypeBuilder<ProgramCourse> builder)
    {
        builder.HasKey(programCourse => new { programCourse.ProgramId, programCourse.CourseId });

        builder.Property(programCourse => programCourse.SequenceOrder)
            .IsRequired();

        builder.HasOne(programCourse => programCourse.Program)
            .WithMany()
            .HasForeignKey(programCourse => programCourse.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(programCourse => programCourse.Course)
            .WithMany()
            .HasForeignKey(programCourse => programCourse.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
