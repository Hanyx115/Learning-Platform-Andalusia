using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AndalusiaAcademy.Infrastructure.Content.Configurations;

public sealed class CmsContentItemConfiguration : IEntityTypeConfiguration<CmsContentItem>
{
    public void Configure(EntityTypeBuilder<CmsContentItem> builder)
    {
        builder.ToTable("CmsContentItems", table =>
            table.HasCheckConstraint("CK_CmsContentItems_DisplayOrder", "[DisplayOrder] >= 0"));

        builder.HasKey(item => item.Id);
        builder.Property(item => item.ContentType).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Key).HasMaxLength(100).IsRequired();
        builder.Property(item => item.TitleEn).HasMaxLength(250).IsRequired();
        builder.Property(item => item.TitleAr).HasMaxLength(250).IsRequired();
        builder.Property(item => item.BodyEn).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.BodyAr).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ImageUrl).HasMaxLength(1000);
        builder.Property(item => item.LinkUrl).HasMaxLength(1000);

        builder.HasIndex(item => new { item.ContentType, item.Key }).IsUnique();
        builder.HasIndex(item => new { item.ContentType, item.IsPublished, item.DisplayOrder });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.RelatedUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        var seededAt = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            Item("b1000000-0000-0000-0000-000000000001", CmsContentTypes.HomepageSection, "hero",
                "Build practical skills for your next professional step", "طوّر مهارات عملية لخطوتك المهنية القادمة",
                "Explore expert-led courses, programs, and career paths designed around real work.", "استكشف دورات وبرامج ومسارات مهنية يقودها خبراء ومصممة حول احتياجات سوق العمل.", 10, seededAt),
            Item("b1000000-0000-0000-0000-000000000002", CmsContentTypes.HomepageSection, "career-paths",
                "Choose a career path with confidence", "اختر مسارك المهني بثقة",
                "See the skills, courses, and programs that can move you forward.", "تعرّف على المهارات والدورات والبرامج التي تساعدك على التقدم.", 20, seededAt),
            Item("b1000000-0000-0000-0000-000000000003", CmsContentTypes.HomepageSection, "categories",
                "Browse by category", "تصفح حسب التصنيف",
                "Start exploring the subjects learners are choosing most.", "ابدأ باستكشاف المجالات الأكثر اختياراً من المتعلمين.", 30, seededAt),
            Item("b1000000-0000-0000-0000-000000000004", CmsContentTypes.HomepageSection, "featured",
                "Featured learning opportunities", "فرص تعلم مميزة",
                "Discover recently published courses and programs.", "اكتشف أحدث الدورات والبرامج المنشورة.", 40, seededAt),
            Item("b1000000-0000-0000-0000-000000000005", CmsContentTypes.HomepageSection, "coming-soon",
                "Coming soon", "قريباً",
                "More practical learning experiences are on the way.", "المزيد من تجارب التعلم العملية في الطريق.", 50, seededAt),
            Item("b1000000-0000-0000-0000-000000000006", CmsContentTypes.HomepageSection, "for-business",
                "Learning solutions for organizations", "حلول تعلم للمؤسسات",
                "Build targeted capability programs for your teams.", "ابنِ برامج تطوير مهارات موجهة لفرقك.", 60, seededAt),
            Item("b1000000-0000-0000-0000-000000000007", CmsContentTypes.HomepageSection, "partners",
                "Trusted by ambitious organizations", "موثوق بنا من مؤسسات طموحة",
                "A demonstration set of organizations represented for the Phase 1 experience.", "مجموعة توضيحية من المؤسسات ضمن تجربة المرحلة الأولى.", 70, seededAt),
            Item("b1000000-0000-0000-0000-000000000008", CmsContentTypes.HomepageSection, "testimonials",
                "What learners say", "ماذا يقول المتعلمون",
                "Stories from learners building practical capability.", "قصص متعلمين يطورون قدراتهم العملية.", 80, seededAt),
            Item("b1000000-0000-0000-0000-000000000009", CmsContentTypes.HomepageSection, "cta",
                "Ready to start learning?", "هل أنت مستعد لبدء التعلم؟",
                "Create your account and explore the right next step.", "أنشئ حسابك واستكشف الخطوة المناسبة التالية.", 90, seededAt),
            Item("b2000000-0000-0000-0000-000000000001", CmsContentTypes.PublicPage, "about",
                "About Andalusia Academy", "عن أكاديمية أندلسية",
                "We connect practical learning with meaningful professional growth.", "نربط التعلم العملي بالنمو المهني الهادف.", 10, seededAt),
            Item("b2000000-0000-0000-0000-000000000002", CmsContentTypes.PublicPage, "contact",
                "Contact and support", "التواصل والدعم",
                "Our team is ready to help you choose and access the right learning opportunity.", "فريقنا جاهز لمساعدتك في اختيار والوصول إلى فرصة التعلم المناسبة.", 20, seededAt),
            Item("b2000000-0000-0000-0000-000000000003", CmsContentTypes.PublicPage, "for-business",
                "Workforce learning that moves with your business", "تعلم للقوى العاملة يواكب أعمالك",
                "Create focused learning experiences for teams and organizations.", "أنشئ تجارب تعلم مركزة للفرق والمؤسسات.", 30, seededAt),
            Item("b3000000-0000-0000-0000-000000000001", CmsContentTypes.Faq, "create-account",
                "How do I create an account?", "كيف أنشئ حساباً؟",
                "Choose Create account, complete the registration form, and sign in with your email and password.", "اختر إنشاء حساب، وأكمل نموذج التسجيل، ثم سجّل الدخول بالبريد الإلكتروني وكلمة المرور.", 10, seededAt),
            Item("b3000000-0000-0000-0000-000000000002", CmsContentTypes.Faq, "apply",
                "How do I apply for a course?", "كيف أقدم على دورة؟",
                "Open a published course, select an available intake, and submit your application while signed in as a learner.", "افتح دورة منشورة، واختر دفعة متاحة، ثم قدم طلبك بعد تسجيل الدخول كمتعلم.", 20, seededAt),
            Item("b3000000-0000-0000-0000-000000000003", CmsContentTypes.Faq, "payment",
                "When can I make a payment?", "متى يمكنني الدفع؟",
                "Payment becomes available after your application is accepted.", "يتاح الدفع بعد قبول طلبك.", 30, seededAt),
            Item("b3000000-0000-0000-0000-000000000004", CmsContentTypes.Faq, "learning-access",
                "When do I receive course access?", "متى أحصل على الوصول إلى الدورة؟",
                "Your learning access is created after a successful payment and active enrollment.", "يتم إنشاء وصولك للتعلم بعد نجاح الدفع وتفعيل التسجيل.", 40, seededAt),
            Item("b3000000-0000-0000-0000-000000000005", CmsContentTypes.Faq, "support",
                "How can I get support?", "كيف يمكنني الحصول على الدعم؟",
                "Use the Contact page to reach the academy support team.", "استخدم صفحة التواصل للوصول إلى فريق دعم الأكاديمية.", 50, seededAt),
            Item("b4000000-0000-0000-0000-000000000001", CmsContentTypes.Partner, "google", "Google", "Google", "Technology partner", "شريك تقني", 10, seededAt),
            Item("b4000000-0000-0000-0000-000000000002", CmsContentTypes.Partner, "amazon", "Amazon", "Amazon", "Technology partner", "شريك تقني", 20, seededAt),
            Item("b4000000-0000-0000-0000-000000000003", CmsContentTypes.Partner, "apple", "Apple", "Apple", "Technology partner", "شريك تقني", 30, seededAt),
            Item("b4000000-0000-0000-0000-000000000004", CmsContentTypes.Partner, "netflix", "Netflix", "Netflix", "Technology partner", "شريك تقني", 40, seededAt),
            Item("b4000000-0000-0000-0000-000000000005", CmsContentTypes.Partner, "meta", "Meta", "Meta", "Technology partner", "شريك تقني", 50, seededAt),
            Item("b4000000-0000-0000-0000-000000000006", CmsContentTypes.Partner, "mango", "MANGO", "MANGO", "Business partner", "شريك أعمال", 60, seededAt),
            Item("b5000000-0000-0000-0000-000000000001", CmsContentTypes.Testimonial, "learner-one",
                "Ahmed - Software learner", "أحمد - متعلم برمجيات",
                "The practical sessions helped me turn theory into work I could demonstrate.", "ساعدتني الجلسات العملية على تحويل المعرفة النظرية إلى عمل يمكنني عرضه.", 10, seededAt),
            Item("b5000000-0000-0000-0000-000000000002", CmsContentTypes.Testimonial, "learner-two",
                "Mariam - Business learner", "مريم - متعلمة أعمال",
                "The program structure made every next step clear and achievable.", "جعل هيكل البرنامج كل خطوة تالية واضحة وقابلة للتحقيق.", 20, seededAt));
    }

    private static CmsContentItem Item(
        string id,
        string contentType,
        string key,
        string titleEn,
        string titleAr,
        string bodyEn,
        string bodyAr,
        int displayOrder,
        DateTimeOffset seededAt)
        => new()
        {
            Id = Guid.Parse(id),
            ContentType = contentType,
            Key = key,
            TitleEn = titleEn,
            TitleAr = titleAr,
            BodyEn = bodyEn,
            BodyAr = bodyAr,
            DisplayOrder = displayOrder,
            IsPublished = true,
            CreatedAtUtc = seededAt,
            UpdatedAtUtc = seededAt,
        };
}
