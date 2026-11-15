using AndalusiaAcademy.Application.DTOs.Scheduling;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Scheduling;

public sealed class RescheduleCourseSessionRequestValidator : AbstractValidator<RescheduleCourseSessionRequest>
{
    public RescheduleCourseSessionRequestValidator()
    {
        RuleFor(request => request.InstructorUserId).NotEmpty();
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.EndsAtUtc)
            .GreaterThan(request => request.StartsAtUtc)
            .WithMessage("The session end time must be after its start time.");
        RuleFor(request => request.Note).MaximumLength(500);
    }
}
