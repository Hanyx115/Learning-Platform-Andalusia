using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments", table =>
        {
            table.HasCheckConstraint(
                "CK_Assignments_ExactlyOneOffering",
                "([CourseOfferingId] IS NOT NULL AND [ProgramOfferingId] IS NULL) OR ([CourseOfferingId] IS NULL AND [ProgramOfferingId] IS NOT NULL)");
            table.HasCheckConstraint("CK_Assignments_MaxScore", "[MaxScore] > 0");
        });

        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Title).HasMaxLength(200).IsRequired();
        builder.Property(assignment => assignment.Description).HasMaxLength(2000);
        builder.Property(assignment => assignment.MaxScore).HasPrecision(6, 2);

        builder.HasIndex(assignment => assignment.CourseOfferingId);
        builder.HasIndex(assignment => assignment.ProgramOfferingId);

        builder.HasOne(assignment => assignment.CourseOffering)
            .WithMany()
            .HasForeignKey(assignment => assignment.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.ProgramOffering)
            .WithMany()
            .HasForeignKey(assignment => assignment.ProgramOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(assignment => assignment.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
