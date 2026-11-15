using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class LearningSkillMappingConfiguration : IEntityTypeConfiguration<LearningSkillMapping>
{
    public void Configure(EntityTypeBuilder<LearningSkillMapping> builder)
    {
        builder.ToTable("LearningSkillMappings", table =>
        {
            table.HasCheckConstraint(
                "CK_LearningSkillMappings_ExactlyOneCatalogItem",
                "([CourseId] IS NOT NULL AND [ProgramId] IS NULL) OR ([CourseId] IS NULL AND [ProgramId] IS NOT NULL)");
            table.HasCheckConstraint("CK_LearningSkillMappings_TargetLevel", "[TargetLevel] BETWEEN 1 AND 5");
        });
        builder.HasKey(mapping => mapping.Id);
        builder.HasOne(mapping => mapping.Skill).WithMany().HasForeignKey(mapping => mapping.SkillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(mapping => mapping.Course).WithMany().HasForeignKey(mapping => mapping.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(mapping => mapping.Program).WithMany().HasForeignKey(mapping => mapping.ProgramId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => new { mapping.SkillId, mapping.CourseId })
            .HasFilter("[CourseId] IS NOT NULL").IsUnique();
        builder.HasIndex(mapping => new { mapping.SkillId, mapping.ProgramId })
            .HasFilter("[ProgramId] IS NOT NULL").IsUnique();
    }
}
