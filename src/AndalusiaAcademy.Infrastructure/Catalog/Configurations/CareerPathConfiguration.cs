using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class CareerPathConfiguration : IEntityTypeConfiguration<CareerPath>
{
    public void Configure(EntityTypeBuilder<CareerPath> builder)
    {
        builder.HasKey(careerPath => careerPath.Id);

        builder.Property(careerPath => careerPath.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(careerPath => careerPath.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(careerPath => careerPath.RecommendedSkills)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(careerPath => careerPath.Status)
            .HasMaxLength(50)
            .IsRequired();
    }
}
