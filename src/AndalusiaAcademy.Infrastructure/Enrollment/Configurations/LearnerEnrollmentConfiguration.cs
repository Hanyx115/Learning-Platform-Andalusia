using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Configurations;

public sealed class LearnerEnrollmentConfiguration : IEntityTypeConfiguration<LearnerEnrollment>
{
    public void Configure(EntityTypeBuilder<LearnerEnrollment> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Enrollments_ExactlyOneOffering",
            "([CourseOfferingId] IS NOT NULL AND [ProgramOfferingId] IS NULL) OR ([CourseOfferingId] IS NULL AND [ProgramOfferingId] IS NOT NULL)"));

        builder.HasKey(enrollment => enrollment.Id);

        builder.Property(enrollment => enrollment.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(enrollment => enrollment.EnrolledAt)
            .IsRequired();

        builder.Property<bool>("IntegrityProtected")
            .HasDefaultValue(true);

        builder.HasOne(enrollment => enrollment.Application)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(enrollment => enrollment.Payment)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(enrollment => enrollment.PaymentId)
            .IsUnique();

        builder.HasOne(enrollment => enrollment.CourseOffering)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.CourseOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(enrollment => enrollment.ProgramOffering)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.ProgramOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(enrollment => enrollment.UserId);

        builder.HasIndex(enrollment => new { enrollment.UserId, enrollment.CourseOfferingId })
            .HasDatabaseName("IX_Enrollments_User_CourseOffering_Active")
            .HasFilter("[IntegrityProtected] = 1 AND [Status] = 'Active' AND [CourseOfferingId] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(enrollment => new { enrollment.UserId, enrollment.ProgramOfferingId })
            .HasDatabaseName("IX_Enrollments_User_ProgramOffering_Active")
            .HasFilter("[IntegrityProtected] = 1 AND [Status] = 'Active' AND [ProgramOfferingId] IS NOT NULL")
            .IsUnique();
    }
}
