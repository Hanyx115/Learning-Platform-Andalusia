using AndalusiaAcademy.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<CourseService>();
        services.AddScoped<ProgramService>();
        services.AddScoped<CareerPathService>();
        services.AddScoped<OfferingService>();
        services.AddScoped<ApplicationSubmissionService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<EnrollmentReadService>();
        services.AddScoped<SchedulingService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<AssignmentService>();
        services.AddScoped<CourseMaterialService>();

        return services;
    }
}
