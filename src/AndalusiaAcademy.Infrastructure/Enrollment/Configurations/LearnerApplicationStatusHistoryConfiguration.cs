using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Enrollment.Configurations;

public sealed class LearnerApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<LearnerApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<LearnerApplicationStatusHistory> builder)
    {
        builder.HasKey(history => history.Id);

        builder.Property(history => history.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(history => history.Note)
            .HasMaxLength(500);

        builder.Property(history => history.ChangedAt)
            .IsRequired();

        builder.HasOne(history => history.Application)
            .WithMany()
            .HasForeignKey(history => history.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => history.ApplicationId);
    }
}
