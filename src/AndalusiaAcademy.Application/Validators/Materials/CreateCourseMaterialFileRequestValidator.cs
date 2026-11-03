using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Domain.Models;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Materials;

public sealed class CreateCourseMaterialFileRequestValidator : AbstractValidator<CreateCourseMaterialFileRequest>
{
    public CreateCourseMaterialFileRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(2000);
        RuleFor(request => request.MaterialType)
            .Must(CourseMaterialTypes.IsSupported)
            .WithMessage($"Material type must be one of: {string.Join(", ", CourseMaterialTypes.All)}.");
        RuleFor(request => request.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Visibility)
            .Must(CourseMaterialVisibilities.IsSupported)
            .WithMessage($"Visibility must be one of: {string.Join(", ", CourseMaterialVisibilities.All)}.");
    }
}
