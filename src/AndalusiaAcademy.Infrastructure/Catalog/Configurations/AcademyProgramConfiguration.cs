using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class AcademyProgramConfiguration : IEntityTypeConfiguration<AcademyProgram>
{
    public void Configure(EntityTypeBuilder<AcademyProgram> builder)
    {
        builder.ToTable("Programs");

        builder.HasKey(program => program.Id);

        builder.Property(program => program.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(program => program.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(program => program.Slug)
            .IsUnique();

        builder.Property(program => program.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(program => program.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(program => program.Price)
            .HasPrecision(10, 2);

        builder.Property(program => program.ImageUrl)
            .HasMaxLength(500);

        builder.Property(program => program.CreatedAt)
            .IsRequired();
    }
}
