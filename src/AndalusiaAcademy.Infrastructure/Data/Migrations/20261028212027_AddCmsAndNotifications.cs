using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AndalusiaAcademy.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCmsAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CmsContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BodyEn = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BodyAr = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LinkUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RelatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsContentItems", x => x.Id);
                    table.CheckConstraint("CK_CmsContentItems_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_CmsContentItems_AspNetUsers_RelatedUserId",
                        column: x => x.RelatedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_CmsContentItems_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MessageEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MessageAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationTemplates_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessages_AttemptCount", "[AttemptCount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceEventKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MessageEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MessageAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_NotificationTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "NotificationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.CheckConstraint("CK_NotificationDeliveries_AttemptCount", "[AttemptCount] >= 1");
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CmsContentItems",
                columns: new[] { "Id", "BodyAr", "BodyEn", "ContentType", "CreatedAtUtc", "DisplayOrder", "ImageUrl", "IsPublished", "Key", "LinkUrl", "RelatedUserId", "TitleAr", "TitleEn", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-0000-0000-000000000001"), "استكشف دورات وبرامج ومسارات مهنية يقودها خبراء ومصممة حول احتياجات سوق العمل.", "Explore expert-led courses, programs, and career paths designed around real work.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 10, null, true, "hero", null, null, "طوّر مهارات عملية لخطوتك المهنية القادمة", "Build practical skills for your next professional step", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000002"), "تعرّف على المهارات والدورات والبرامج التي تساعدك على التقدم.", "See the skills, courses, and programs that can move you forward.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 20, null, true, "career-paths", null, null, "اختر مسارك المهني بثقة", "Choose a career path with confidence", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000003"), "ابدأ باستكشاف المجالات الأكثر اختياراً من المتعلمين.", "Start exploring the subjects learners are choosing most.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, null, true, "categories", null, null, "تصفح حسب التصنيف", "Browse by category", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000004"), "اكتشف أحدث الدورات والبرامج المنشورة.", "Discover recently published courses and programs.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 40, null, true, "featured", null, null, "فرص تعلم مميزة", "Featured learning opportunities", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000005"), "المزيد من تجارب التعلم العملية في الطريق.", "More practical learning experiences are on the way.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 50, null, true, "coming-soon", null, null, "قريباً", "Coming soon", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000006"), "ابنِ برامج تطوير مهارات موجهة لفرقك.", "Build targeted capability programs for your teams.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 60, null, true, "for-business", null, null, "حلول تعلم للمؤسسات", "Learning solutions for organizations", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000007"), "مجموعة توضيحية من المؤسسات ضمن تجربة المرحلة الأولى.", "A demonstration set of organizations represented for the Phase 1 experience.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 70, null, true, "partners", null, null, "موثوق بنا من مؤسسات طموحة", "Trusted by ambitious organizations", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000008"), "قصص متعلمين يطورون قدراتهم العملية.", "Stories from learners building practical capability.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 80, null, true, "testimonials", null, null, "ماذا يقول المتعلمون", "What learners say", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b1000000-0000-0000-0000-000000000009"), "أنشئ حسابك واستكشف الخطوة المناسبة التالية.", "Create your account and explore the right next step.", "HomepageSection", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 90, null, true, "cta", null, null, "هل أنت مستعد لبدء التعلم؟", "Ready to start learning?", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b2000000-0000-0000-0000-000000000001"), "نربط التعلم العملي بالنمو المهني الهادف.", "We connect practical learning with meaningful professional growth.", "PublicPage", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 10, null, true, "about", null, null, "عن أكاديمية أندلسية", "About Andalusia Academy", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b2000000-0000-0000-0000-000000000002"), "فريقنا جاهز لمساعدتك في اختيار والوصول إلى فرصة التعلم المناسبة.", "Our team is ready to help you choose and access the right learning opportunity.", "PublicPage", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 20, null, true, "contact", null, null, "التواصل والدعم", "Contact and support", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b2000000-0000-0000-0000-000000000003"), "أنشئ تجارب تعلم مركزة للفرق والمؤسسات.", "Create focused learning experiences for teams and organizations.", "PublicPage", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, null, true, "for-business", null, null, "تعلم للقوى العاملة يواكب أعمالك", "Workforce learning that moves with your business", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b3000000-0000-0000-0000-000000000001"), "اختر إنشاء حساب، وأكمل نموذج التسجيل، ثم سجّل الدخول بالبريد الإلكتروني وكلمة المرور.", "Choose Create account, complete the registration form, and sign in with your email and password.", "Faq", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 10, null, true, "create-account", null, null, "كيف أنشئ حساباً؟", "How do I create an account?", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b3000000-0000-0000-0000-000000000002"), "افتح دورة منشورة، واختر دفعة متاحة، ثم قدم طلبك بعد تسجيل الدخول كمتعلم.", "Open a published course, select an available intake, and submit your application while signed in as a learner.", "Faq", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 20, null, true, "apply", null, null, "كيف أقدم على دورة؟", "How do I apply for a course?", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b3000000-0000-0000-0000-000000000003"), "يتاح الدفع بعد قبول طلبك.", "Payment becomes available after your application is accepted.", "Faq", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, null, true, "payment", null, null, "متى يمكنني الدفع؟", "When can I make a payment?", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b3000000-0000-0000-0000-000000000004"), "يتم إنشاء وصولك للتعلم بعد نجاح الدفع وتفعيل التسجيل.", "Your learning access is created after a successful payment and active enrollment.", "Faq", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 40, null, true, "learning-access", null, null, "متى أحصل على الوصول إلى الدورة؟", "When do I receive course access?", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b3000000-0000-0000-0000-000000000005"), "استخدم صفحة التواصل للوصول إلى فريق دعم الأكاديمية.", "Use the Contact page to reach the academy support team.", "Faq", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 50, null, true, "support", null, null, "كيف يمكنني الحصول على الدعم؟", "How can I get support?", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b4000000-0000-0000-0000-000000000001"), "شريك تقني", "Technology partner", "Partner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 10, null, true, "google", null, null, "Google", "Google", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b4000000-0000-0000-0000-000000000002"), "شريك تقني", "Technology partner", "Partner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 20, null, true, "amazon", null, null, "Amazon", "Amazon", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b4000000-0000-0000-0000-000000000003"), "شريك تقني", "Technology partner", "Partner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, null, true, "apple", null, null, "Apple", "Apple", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b4000000-0000-0000-0000-000000000004"), "شريك تقني", "Technology partner", "Partner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 40, null, true, "netflix", null, null, "Netflix", "Netflix", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b4000000-0000-0000-0000-000000000005"), "شريك تقني", "Technology partner", "Partner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 50, null, true, "meta", null, null, "Meta", "Meta", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b4000000-0000-0000-0000-000000000006"), "شريك أعمال", "Business partner", "Partner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 60, null, true, "mango", null, null, "MANGO", "MANGO", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b5000000-0000-0000-0000-000000000001"), "ساعدتني الجلسات العملية على تحويل المعرفة النظرية إلى عمل يمكنني عرضه.", "The practical sessions helped me turn theory into work I could demonstrate.", "Testimonial", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 10, null, true, "learner-one", null, null, "أحمد - متعلم برمجيات", "Ahmed - Software learner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("b5000000-0000-0000-0000-000000000002"), "جعل هيكل البرنامج كل خطوة تالية واضحة وقابلة للتحقيق.", "The program structure made every next step clear and achievable.", "Testimonial", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 20, null, true, "learner-two", null, null, "مريم - متعلمة أعمال", "Mariam - Business learner", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Id", "Code", "IsActive", "MessageAr", "MessageEn", "TitleAr", "TitleEn", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), "AccountCreated", true, "حسابك كمتعلم جاهز الآن.", "Your learner account is ready.", "مرحباً بك في أكاديمية أندلسية", "Welcome to Andalusia Academy", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), "ApplicationSubmitted", true, "تم تقديم طلبك للالتحاق بـ {{offering}} بنجاح.", "Your application for {{offering}} was submitted successfully.", "تم استلام طلبك", "Application received", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), "ApplicationStatusUpdated", true, "أصبحت حالة طلبك الآن {{status}}.", "Your application status is now {{status}}.", "تم تحديث حالة الطلب", "Application status updated", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), "PaymentConfirmed", true, "تم استلام دفعتك بقيمة {{amount}} {{currency}}.", "We received your payment of {{amount}} {{currency}}.", "تم تأكيد الدفع", "Payment confirmed", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000005"), "EnrollmentConfirmed", true, "تم تسجيلك الآن في {{offering}}.", "You are now enrolled in {{offering}}.", "تم تأكيد التسجيل", "Enrollment confirmed", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000006"), "SessionReminder", true, "تبدأ جلسة {{sessionTitle}} في {{startsAt}}.", "{{sessionTitle}} starts at {{startsAt}}.", "جلسة قادمة", "Upcoming session", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000007"), "CourseAnnouncement", true, "يتوفر إعلان جديد للدورة.", "A new course announcement is available.", "إعلان الدورة", "Course announcement", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000008"), "InstructorAssigned", true, "تم إسناد جلسة {{sessionTitle}} إليك في {{startsAt}}.", "You were assigned to {{sessionTitle}} at {{startsAt}}.", "تم إسناد جلسة", "Session assigned", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000009"), "NewLearnerEnrollment", true, "تم تسجيل {{learnerName}} في {{offering}}.", "{{learnerName}} enrolled in {{offering}}.", "تسجيل متعلم جديد", "New learner enrollment", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000010"), "AssignmentSubmitted", true, "قام {{learnerName}} بتسليم {{assignmentTitle}}.", "{{learnerName}} submitted {{assignmentTitle}}.", "تم تسليم التكليف", "Assignment submitted", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null },
                    { new Guid("a1000000-0000-0000-0000-000000000011"), "Operational", true, "{{message}}", "{{message}}", "تحديث تشغيلي", "Operational update", new DateTimeOffset(new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentItems_ContentType_IsPublished_DisplayOrder",
                table: "CmsContentItems",
                columns: new[] { "ContentType", "IsPublished", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentItems_ContentType_Key",
                table: "CmsContentItems",
                columns: new[] { "ContentType", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentItems_RelatedUserId",
                table: "CmsContentItems",
                column: "RelatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CmsContentItems_UpdatedByUserId",
                table: "CmsContentItems",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationId_Channel",
                table: "NotificationDeliveries",
                columns: new[] { "NotificationId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TemplateId",
                table: "Notifications",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_ReadAtUtc_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "ReadAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_SourceEventKey",
                table: "Notifications",
                columns: new[] { "UserId", "SourceEventKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Code",
                table: "NotificationTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_UpdatedByUserId",
                table: "NotificationTemplates",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_EventKey",
                table: "OutboxMessages",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_NextAttemptAtUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CmsContentItems");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");
        }
    }
}
