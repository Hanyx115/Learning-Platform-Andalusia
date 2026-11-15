using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Notifications.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", table =>
            table.HasCheckConstraint("CK_OutboxMessages_AttemptCount", "[AttemptCount] >= 0"));
        builder.HasKey(message => message.Id);
        builder.Property(message => message.EventKey).HasMaxLength(250).IsRequired();
        builder.Property(message => message.EventType).HasMaxLength(100).IsRequired();
        builder.Property(message => message.PayloadJson).HasMaxLength(8000).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2000);
        builder.HasIndex(message => message.EventKey).IsUnique();
        builder.HasIndex(message => new { message.ProcessedAtUtc, message.NextAttemptAtUtc, message.OccurredAtUtc });
    }
}
