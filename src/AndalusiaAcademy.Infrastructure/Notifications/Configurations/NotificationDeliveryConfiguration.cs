using AndalusiaAcademy.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Notifications.Configurations;

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries", table =>
            table.HasCheckConstraint("CK_NotificationDeliveries_AttemptCount", "[AttemptCount] >= 1"));
        builder.HasKey(delivery => delivery.Id);
        builder.Property(delivery => delivery.Channel).HasMaxLength(30).IsRequired();
        builder.Property(delivery => delivery.Status).HasMaxLength(30).IsRequired();
        builder.Property(delivery => delivery.FailureReason).HasMaxLength(1000);
        builder.HasIndex(delivery => new { delivery.NotificationId, delivery.Channel }).IsUnique();
        builder.HasOne(delivery => delivery.Notification)
            .WithMany(notification => notification.Deliveries)
            .HasForeignKey(delivery => delivery.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
