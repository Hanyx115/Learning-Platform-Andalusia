using AndalusiaAcademy.Application.DTOs.Assignments;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Assignments;

public sealed class GradeSubmissionRequestValidator : AbstractValidator<GradeSubmissionRequest>
{
    public GradeSubmissionRequestValidator()
    {
        RuleFor(request => request.Score).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Feedback).MaximumLength(2000);
    }
}
