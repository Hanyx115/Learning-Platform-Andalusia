using AndalusiaAcademy.Application.DTOs.Assignments;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Assignments;

public sealed class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(2000);
        RuleFor(request => request.MaxScore).GreaterThan(0);
    }
}
