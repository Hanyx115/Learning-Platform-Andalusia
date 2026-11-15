using AndalusiaAcademy.Application.DTOs.Scheduling;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Scheduling;

public sealed class CancelCourseSessionRequestValidator : AbstractValidator<CancelCourseSessionRequest>
{
    public CancelCourseSessionRequestValidator()
    {
        RuleFor(request => request.Reason).NotEmpty().MaximumLength(500);
    }
}
