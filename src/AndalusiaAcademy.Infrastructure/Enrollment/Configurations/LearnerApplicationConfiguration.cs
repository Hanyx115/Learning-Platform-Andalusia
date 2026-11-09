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

        builder.Property<bool>("IntegrityProtected")
            .HasDefaultValue(true);

        builder.HasOne(application => application.CourseOffering)
            .WithMany()
            .HasForeignKey(application => application.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.ProgramOffering)
            .WithMany()
            .HasForeignKey(application => application.ProgramOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Exactly one concrete intake must be selected.
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_LearnerApplications_ExactlyOneTarget",
            "([CourseOfferingId] IS NOT NULL AND [ProgramOfferingId] IS NULL) OR ([CourseOfferingId] IS NULL AND [ProgramOfferingId] IS NOT NULL)"));

        // A learner can only have one open application per offering at a time. Rejected applications
        // do not block a corrected reapplication, while an accepted application remains the payment
        // source of truth and must be reused rather than duplicated.
        builder.HasIndex(application => new { application.UserId, application.CourseOfferingId })
            .HasDatabaseName("IX_LearnerApplications_User_CourseOffering_Open")
            .HasFilter("[IntegrityProtected] = 1 AND [Status] IN ('Pending', 'Accepted') AND [CourseOfferingId] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(application => new { application.UserId, application.ProgramOfferingId })
            .HasDatabaseName("IX_LearnerApplications_User_ProgramOffering_Open")
            .HasFilter("[IntegrityProtected] = 1 AND [Status] IN ('Pending', 'Accepted') AND [ProgramOfferingId] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(application => application.Status);
    }
}
