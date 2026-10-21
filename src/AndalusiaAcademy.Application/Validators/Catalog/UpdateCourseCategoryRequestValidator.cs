using AndalusiaAcademy.Application.DTOs;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Catalog;

public sealed class UpdateCourseCategoryRequestValidator : AbstractValidator<UpdateCourseCategoryRequest>
{
    public UpdateCourseCategoryRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
    }
}
