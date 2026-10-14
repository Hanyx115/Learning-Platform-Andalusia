using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Configurations;

public sealed class LearnerApplicationConfiguration : IEntityTypeConfiguration<LearnerApplication>
{
    public void Configure(EntityTypeBuilder<LearnerApplication> builder)
    {
        builder.HasKey(application => application.Id);

        builder.Property(application => application.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(application => application.RejectionReason)
            .HasMaxLength(500);

        builder.Property(application => application.SubmittedAt)
            .IsRequired();

        builder.HasOne(application => application.Course)
            .WithMany()
            .HasForeignKey(application => application.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.Program)
            .WithMany()
            .HasForeignKey(application => application.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        // Exactly one of Course/Program must be set: a learner applies to one or the other, never both or neither.
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_LearnerApplications_ExactlyOneTarget",
            "([CourseId] IS NOT NULL AND [ProgramId] IS NULL) OR ([CourseId] IS NULL AND [ProgramId] IS NOT NULL)"));

        // A learner can only have one open application per course/program at a time; a new one after
        // rejection is fine since the filter only covers the Pending status.
        builder.HasIndex(application => new { application.UserId, application.CourseId })
            .HasDatabaseName("IX_LearnerApplications_User_Course_Pending")
            .HasFilter("[Status] = 'Pending' AND [CourseId] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(application => new { application.UserId, application.ProgramId })
            .HasDatabaseName("IX_LearnerApplications_User_Program_Pending")
            .HasFilter("[Status] = 'Pending' AND [ProgramId] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(application => application.Status);
    }
}
