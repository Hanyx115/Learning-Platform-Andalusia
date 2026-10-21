using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Catalog.Configurations;

public sealed class ProgramSessionConfiguration : IEntityTypeConfiguration<ProgramSession>
{
    public void Configure(EntityTypeBuilder<ProgramSession> builder)
    {
        builder.ToTable("ProgramSessions", table =>
        {
            table.HasCheckConstraint("CK_ProgramSessions_Schedule", "[StartsAtUtc] < [EndsAtUtc]");
            table.HasCheckConstraint("CK_ProgramSessions_Status", "[Status] IN ('Scheduled', 'Completed', 'Cancelled')");
            table.HasCheckConstraint("CK_ProgramSessions_Cancellation", "([Status] = 'Cancelled' AND [CancellationReason] IS NOT NULL) OR ([Status] <> 'Cancelled' AND [CancellationReason] IS NULL)");
        });

        builder.HasKey(session => session.Id);
        builder.Property(session => session.Title).HasMaxLength(200).IsRequired();
        builder.Property(session => session.Status).HasMaxLength(32).IsRequired();
        builder.Property(session => session.CancellationReason).HasMaxLength(500);
        builder.Property(session => session.ReschedulingNote).HasMaxLength(500);
        builder.HasIndex(session => new { session.ProgramOfferingId, session.StartsAtUtc }).IsUnique();
        builder.HasIndex(session => new { session.LocationId, session.StartsAtUtc, session.EndsAtUtc });
        builder.HasIndex(session => new { session.InstructorUserId, session.StartsAtUtc, session.EndsAtUtc });
        builder.HasIndex(session => new { session.Status, session.StartsAtUtc });
        builder.HasOne(session => session.ProgramOffering).WithMany().HasForeignKey(session => session.ProgramOfferingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(session => session.Location).WithMany().HasForeignKey(session => session.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(session => session.InstructorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
