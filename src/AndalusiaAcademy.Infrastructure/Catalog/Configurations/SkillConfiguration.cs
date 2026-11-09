using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(skill => skill.Id);
        builder.Property(skill => skill.Name).HasMaxLength(120).IsRequired();
        builder.Property(skill => skill.Category).HasMaxLength(100).IsRequired();
        builder.Property(skill => skill.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(skill => skill.Name).IsUnique();
    }
}
