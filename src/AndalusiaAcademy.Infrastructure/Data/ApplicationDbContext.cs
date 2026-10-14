using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Identity.Models;
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

    public DbSet<ProgramCourse> ProgramCourses => Set<ProgramCourse>();

    public DbSet<CareerPath> CareerPaths => Set<CareerPath>();

    public DbSet<CareerPathProgram> CareerPathPrograms => Set<CareerPathProgram>();

    public DbSet<CareerPathCourse> CareerPathCourses => Set<CareerPathCourse>();

    public DbSet<LearnerApplication> LearnerApplications => Set<LearnerApplication>();

    public DbSet<LearnerApplicationStatusHistory> LearnerApplicationStatusHistories => Set<LearnerApplicationStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
