using AndalusiaAcademy.Application.DTOs.Assignments;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Assignments;

public sealed class SubmitAssignmentRequestValidator : AbstractValidator<SubmitAssignmentRequest>
{
    public SubmitAssignmentRequestValidator()
    {
        RuleFor(request => request.Content).NotEmpty().MaximumLength(2000);
    }
}
