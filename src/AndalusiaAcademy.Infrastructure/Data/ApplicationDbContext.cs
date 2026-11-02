using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<AcademyProgram> Programs => Set<AcademyProgram>();

    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();

    public DbSet<ProgramOffering> ProgramOfferings => Set<ProgramOffering>();

    public DbSet<TrainingLocation> TrainingLocations => Set<TrainingLocation>();

    public DbSet<CourseSession> CourseSessions => Set<CourseSession>();

    public DbSet<ProgramSession> ProgramSessions => Set<ProgramSession>();

    public DbSet<CourseSessionAttendance> CourseSessionAttendances => Set<CourseSessionAttendance>();

    public DbSet<ProgramSessionAttendance> ProgramSessionAttendances => Set<ProgramSessionAttendance>();

    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();

    public DbSet<ProgramCourse> ProgramCourses => Set<ProgramCourse>();

    public DbSet<CareerPath> CareerPaths => Set<CareerPath>();

    public DbSet<CareerPathProgram> CareerPathPrograms => Set<CareerPathProgram>();

    public DbSet<CareerPathCourse> CareerPathCourses => Set<CareerPathCourse>();

    public DbSet<LearnerApplication> LearnerApplications => Set<LearnerApplication>();

    public DbSet<LearnerApplicationStatusHistory> LearnerApplicationStatusHistories => Set<LearnerApplicationStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentStatusHistory> PaymentStatusHistories => Set<PaymentStatusHistory>();

    public DbSet<LearnerEnrollment> Enrollments => Set<LearnerEnrollment>();

    public DbSet<CmsContentItem> CmsContentItems => Set<CmsContentItem>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();

    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    public DbSet<InstructorCourseRate> InstructorCourseRates => Set<InstructorCourseRate>();

    public DbSet<StaffCompensationRate> StaffCompensationRates => Set<StaffCompensationRate>();

    public DbSet<CompensationPayout> CompensationPayouts => Set<CompensationPayout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Authorization is role-based and hardcoded in policy checks (not claims-driven),
        // so Identity's generic claims tables are intentionally excluded from the model.
        modelBuilder.Ignore<IdentityUserClaim<Guid>>();
        modelBuilder.Ignore<IdentityRoleClaim<Guid>>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
