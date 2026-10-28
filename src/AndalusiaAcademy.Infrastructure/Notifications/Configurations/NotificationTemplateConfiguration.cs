using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Notifications.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.Code).HasMaxLength(100).IsRequired();
        builder.Property(template => template.TitleEn).HasMaxLength(250).IsRequired();
        builder.Property(template => template.TitleAr).HasMaxLength(250).IsRequired();
        builder.Property(template => template.MessageEn).HasMaxLength(2000).IsRequired();
        builder.Property(template => template.MessageAr).HasMaxLength(2000).IsRequired();
        builder.HasIndex(template => template.Code).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(template => template.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        var seededAt = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            Template("a1000000-0000-0000-0000-000000000001", NotificationTemplateCodes.AccountCreated,
                "Welcome to Andalusia Academy", "مرحباً بك في أكاديمية أندلسية",
                "Your learner account is ready.", "حسابك كمتعلم جاهز الآن.", seededAt),
            Template("a1000000-0000-0000-0000-000000000002", NotificationTemplateCodes.ApplicationSubmitted,
                "Application received", "تم استلام طلبك",
                "Your application for {{offering}} was submitted successfully.", "تم تقديم طلبك للالتحاق بـ {{offering}} بنجاح.", seededAt),
            Template("a1000000-0000-0000-0000-000000000003", NotificationTemplateCodes.ApplicationStatusUpdated,
                "Application status updated", "تم تحديث حالة الطلب",
                "Your application status is now {{status}}.", "أصبحت حالة طلبك الآن {{status}}.", seededAt),
            Template("a1000000-0000-0000-0000-000000000004", NotificationTemplateCodes.PaymentConfirmed,
                "Payment confirmed", "تم تأكيد الدفع",
                "We received your payment of {{amount}} {{currency}}.", "تم استلام دفعتك بقيمة {{amount}} {{currency}}.", seededAt),
            Template("a1000000-0000-0000-0000-000000000005", NotificationTemplateCodes.EnrollmentConfirmed,
                "Enrollment confirmed", "تم تأكيد التسجيل",
                "You are now enrolled in {{offering}}.", "تم تسجيلك الآن في {{offering}}.", seededAt),
            Template("a1000000-0000-0000-0000-000000000006", NotificationTemplateCodes.SessionReminder,
                "Upcoming session", "جلسة قادمة",
                "{{sessionTitle}} starts at {{startsAt}}.", "تبدأ جلسة {{sessionTitle}} في {{startsAt}}.", seededAt),
            Template("a1000000-0000-0000-0000-000000000007", NotificationTemplateCodes.CourseAnnouncement,
                "Course announcement", "إعلان الدورة",
                "A new course announcement is available.", "يتوفر إعلان جديد للدورة.", seededAt),
            Template("a1000000-0000-0000-0000-000000000008", NotificationTemplateCodes.InstructorAssigned,
                "Session assigned", "تم إسناد جلسة",
                "You were assigned to {{sessionTitle}} at {{startsAt}}.", "تم إسناد جلسة {{sessionTitle}} إليك في {{startsAt}}.", seededAt),
            Template("a1000000-0000-0000-0000-000000000009", NotificationTemplateCodes.NewLearnerEnrollment,
                "New learner enrollment", "تسجيل متعلم جديد",
                "{{learnerName}} enrolled in {{offering}}.", "تم تسجيل {{learnerName}} في {{offering}}.", seededAt),
            Template("a1000000-0000-0000-0000-000000000010", NotificationTemplateCodes.AssignmentSubmitted,
                "Assignment submitted", "تم تسليم التكليف",
                "{{learnerName}} submitted {{assignmentTitle}}.", "قام {{learnerName}} بتسليم {{assignmentTitle}}.", seededAt),
            Template("a1000000-0000-0000-0000-000000000011", NotificationTemplateCodes.Operational,
                "Operational update", "تحديث تشغيلي",
                "{{message}}", "{{message}}", seededAt));
    }

    private static NotificationTemplate Template(
        string id,
        string code,
        string titleEn,
        string titleAr,
        string messageEn,
        string messageAr,
        DateTimeOffset updatedAtUtc)
        => new()
        {
            Id = Guid.Parse(id),
            Code = code,
            TitleEn = titleEn,
            TitleAr = titleAr,
            MessageEn = messageEn,
            MessageAr = messageAr,
            IsActive = true,
            UpdatedAtUtc = updatedAtUtc,
        };
}
