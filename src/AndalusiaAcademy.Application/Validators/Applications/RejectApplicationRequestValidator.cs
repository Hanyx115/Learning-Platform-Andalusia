using AndalusiaAcademy.Application.DTOs.Applications;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Applications;

public sealed class RejectApplicationRequestValidator : AbstractValidator<RejectApplicationRequest>
{
    public RejectApplicationRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}
