using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Configurations;

public sealed class LearnerEnrollmentConfiguration : IEntityTypeConfiguration<LearnerEnrollment>
{
    public void Configure(EntityTypeBuilder<LearnerEnrollment> builder)
    {
        builder.HasKey(enrollment => enrollment.Id);

        builder.Property(enrollment => enrollment.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(enrollment => enrollment.EnrolledAt)
            .IsRequired();

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

        builder.HasOne(enrollment => enrollment.Course)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(enrollment => enrollment.Program)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(enrollment => enrollment.UserId);
    }
}
