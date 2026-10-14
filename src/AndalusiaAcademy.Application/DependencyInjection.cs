using AndalusiaAcademy.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<CourseService>();
        services.AddScoped<ProgramService>();
        services.AddScoped<CareerPathService>();
        services.AddScoped<ApplicationSubmissionService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<EnrollmentReadService>();

        return services;
    }
}
