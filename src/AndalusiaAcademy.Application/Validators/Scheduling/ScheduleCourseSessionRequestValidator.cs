using AndalusiaAcademy.Application.DTOs.Scheduling;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Scheduling;

public sealed class ScheduleCourseSessionRequestValidator : AbstractValidator<ScheduleCourseSessionRequest>
{
    public ScheduleCourseSessionRequestValidator()
    {
        RuleFor(request => request.LocationId).NotEmpty();
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.EndsAtUtc)
            .GreaterThan(request => request.StartsAtUtc)
            .WithMessage("The session end time must be after its start time.");
    }
}
