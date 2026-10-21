using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Lms.Configurations;

public sealed class CourseMaterialConfiguration : IEntityTypeConfiguration<CourseMaterial>
{
    public void Configure(EntityTypeBuilder<CourseMaterial> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CourseMaterials_MaterialType",
                "[MaterialType] IN ('Document', 'Video', 'Link', 'Other')");
            table.HasCheckConstraint(
                "CK_CourseMaterials_Visibility",
                "[Visibility] IN ('Published', 'Hidden')");
            table.HasCheckConstraint(
                "CK_CourseMaterials_SortOrder",
                "[SortOrder] >= 0");
        });

        builder.HasKey(material => material.Id);

        builder.Property(material => material.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(material => material.Description)
            .HasMaxLength(2000);

        builder.Property(material => material.ResourceUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(material => material.MaterialType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(material => material.Visibility)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(material => material.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(material => material.Course)
            .WithMany()
            .HasForeignKey(material => material.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(material => material.CourseSession)
            .WithMany()
            .HasForeignKey(material => material.CourseSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(material => material.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(material => new
        {
            material.CourseId,
            material.Visibility,
            material.SortOrder,
        });

        builder.HasIndex(material => material.CourseSessionId);
    }
}
