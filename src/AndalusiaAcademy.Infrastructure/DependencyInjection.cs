using System.Security.Claims;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Catalog.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Enrollment.Repositories;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Infrastructure.Enrollment.Services;
using AndalusiaAcademy.Infrastructure.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using AndalusiaAcademy.Infrastructure.Identity.Repositories;
using AndalusiaAcademy.Infrastructure.Identity.Services;
using AndalusiaAcademy.Infrastructure.Lms.Repositories;
using AndalusiaAcademy.Infrastructure.Lms.Storage;
using AndalusiaAcademy.Infrastructure.Content.Repositories;
using AndalusiaAcademy.Infrastructure.Notifications;
using AndalusiaAcademy.Infrastructure.Notifications.Repositories;
using AndalusiaAcademy.Application.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AndalusiaAcademy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDataProtection();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is required when the database context is used.");
            }

            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlServerOptions.EnableRetryOnFailure();
            });
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;

                // Off until a real email provider exists (decided 2026-10-13); revisit when notifications/email are built.
                options.SignIn.RequireConfirmedEmail = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(2));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                jwtOptions => !string.IsNullOrWhiteSpace(jwtOptions.Key),
                "Jwt:Key is required. Set it via User Secrets locally or secure configuration in deployment.")
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = string.IsNullOrWhiteSpace(jwtOptions.Key)
                        ? null
                        : new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdValue = context.Principal?
                            .FindFirst(ClaimTypes.NameIdentifier)?
                            .Value;

                        if (!Guid.TryParse(userIdValue, out var userId))
                        {
                            context.Fail("The authenticated user identifier is invalid.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await userManager.FindByIdAsync(userId.ToString());

                        if (user is null || !user.IsActive)
                        {
                            context.Fail("This account is inactive.");
                        }
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.RequireLearner, policy => policy.RequireRole(RoleNames.Learner));
            options.AddPolicy(PolicyNames.RequireAdmin, policy => policy.RequireRole(RoleNames.Admin));
            options.AddPolicy(
                PolicyNames.RequireManagerOrAdmin,
                policy => policy.RequireRole(RoleNames.Manager, RoleNames.Admin));
            options.AddPolicy(PolicyNames.RequireInstructor, policy => policy.RequireRole(RoleNames.Instructor));
            options.AddPolicy(
                PolicyNames.RequireInstructorManagerOrAdmin,
                policy => policy.RequireRole(RoleNames.Instructor, RoleNames.Manager, RoleNames.Admin));
            options.AddPolicy(
                PolicyNames.RequireContentManager,
                policy => policy.RequireRole(RoleNames.ContentManager));
            options.AddPolicy(
                PolicyNames.RequireContentManagerOrAdmin,
                policy => policy.RequireRole(RoleNames.ContentManager, RoleNames.Admin));
            options.AddPolicy(
                PolicyNames.RequireContentManagerManagerOrAdmin,
                policy => policy.RequireRole(RoleNames.ContentManager, RoleNames.Manager, RoleNames.Admin));
        });

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAdminUserService, AdminUserService>();

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseCategoryRepository, CourseCategoryRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<ICareerPathRepository, CareerPathRepository>();
        services.AddScoped<IOfferingRepository, OfferingRepository>();
        services.AddScoped<ISchedulingRepository, SchedulingRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddSingleton<IAssignmentSubmissionFileStore, LocalAssignmentSubmissionFileStore>();
        services.AddScoped<IProgressRepository, ProgressRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IPaymentProcessor, SimulatedPaymentProcessor>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ICourseMaterialRepository, CourseMaterialRepository>();
        services.AddScoped<ICmsRepository, CmsRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationOutbox, NotificationOutbox>();
        services.AddSingleton<NotificationOutboxProcessor>();
        services.AddHostedService(provider => provider.GetRequiredService<NotificationOutboxProcessor>());

        return services;
    }
}
