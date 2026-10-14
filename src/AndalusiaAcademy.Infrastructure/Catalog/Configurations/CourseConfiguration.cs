using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(course => course.Id);

        builder.Property(course => course.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(course => course.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(course => course.Slug)
            .IsUnique();

        builder.Property(course => course.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(course => course.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(course => course.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(course => course.Price)
            .HasPrecision(10, 2);

        builder.Property(course => course.CreatedAt)
            .IsRequired();

        builder.HasOne(course => course.Category)
            .WithMany()
            .HasForeignKey(course => course.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
