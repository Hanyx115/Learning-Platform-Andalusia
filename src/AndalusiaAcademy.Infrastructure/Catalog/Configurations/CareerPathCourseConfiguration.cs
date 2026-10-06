using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class CareerPathCourseConfiguration : IEntityTypeConfiguration<CareerPathCourse>
{
    public void Configure(EntityTypeBuilder<CareerPathCourse> builder)
    {
        builder.HasKey(link => new { link.CareerPathId, link.CourseId });

        builder.HasOne(link => link.CareerPath)
            .WithMany()
            .HasForeignKey(link => link.CareerPathId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Course)
            .WithMany()
            .HasForeignKey(link => link.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
