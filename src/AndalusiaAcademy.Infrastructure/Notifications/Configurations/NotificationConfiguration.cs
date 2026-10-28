using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Notifications.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.EventType).HasMaxLength(100).IsRequired();
        builder.Property(notification => notification.SourceEventKey).HasMaxLength(250).IsRequired();
        builder.Property(notification => notification.TitleEn).HasMaxLength(250).IsRequired();
        builder.Property(notification => notification.TitleAr).HasMaxLength(250).IsRequired();
        builder.Property(notification => notification.MessageEn).HasMaxLength(2000).IsRequired();
        builder.Property(notification => notification.MessageAr).HasMaxLength(2000).IsRequired();
        builder.Property(notification => notification.LinkUrl).HasMaxLength(1000);
        builder.HasIndex(notification => new { notification.UserId, notification.ReadAtUtc, notification.CreatedAtUtc });
        builder.HasIndex(notification => new { notification.UserId, notification.SourceEventKey }).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(notification => notification.Template)
            .WithMany()
            .HasForeignKey(notification => notification.TemplateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
