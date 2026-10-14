using AndalusiaAcademy.Application.DTOs.Applications;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Applications;

public sealed class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.CourseId.HasValue ^ request.ProgramId.HasValue)
            .WithMessage("Exactly one of CourseId or ProgramId must be provided.");
    }
}
