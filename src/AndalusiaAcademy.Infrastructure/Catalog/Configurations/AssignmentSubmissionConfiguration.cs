using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("AssignmentSubmissions", table =>
        {
            table.HasCheckConstraint(
                "CK_AssignmentSubmissions_Grading",
                "([Score] IS NULL AND [GradedByUserId] IS NULL AND [GradedAtUtc] IS NULL) OR ([Score] IS NOT NULL AND [GradedByUserId] IS NOT NULL AND [GradedAtUtc] IS NOT NULL)");
            table.HasCheckConstraint("CK_AssignmentSubmissions_ScoreNonNegative", "[Score] IS NULL OR [Score] >= 0");
        });

        builder.HasKey(submission => submission.Id);
        builder.Property(submission => submission.Content).HasMaxLength(2000).IsRequired();
        builder.Property(submission => submission.Score).HasPrecision(6, 2);
        builder.Property(submission => submission.Feedback).HasMaxLength(2000);

        builder.HasIndex(submission => new { submission.AssignmentId, submission.UserId }).IsUnique();

        builder.HasOne(submission => submission.Assignment)
            .WithMany()
            .HasForeignKey(submission => submission.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(submission => submission.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(submission => submission.GradedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
