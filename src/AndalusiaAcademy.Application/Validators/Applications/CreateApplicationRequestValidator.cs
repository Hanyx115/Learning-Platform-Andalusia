using AndalusiaAcademy.Application.DTOs.Applications;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Applications;

public sealed class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.CourseOfferingId.HasValue ^ request.ProgramOfferingId.HasValue)
            .WithMessage("Exactly one of CourseOfferingId or ProgramOfferingId must be provided.");
    }
}
