using AndalusiaAcademy.Application.DTOs;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Catalog;

public sealed class CreateCourseCategoryRequestValidator : AbstractValidator<CreateCourseCategoryRequest>
{
    public CreateCourseCategoryRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
    }
}
