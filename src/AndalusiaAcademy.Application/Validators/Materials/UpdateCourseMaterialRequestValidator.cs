using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Domain.Models;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Materials;

public sealed class UpdateCourseMaterialRequestValidator : AbstractValidator<UpdateCourseMaterialRequest>
{
    public UpdateCourseMaterialRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(2000);
        RuleFor(request => request.ResourceUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(IsHttpUrl)
            .WithMessage("Resource URL must be an absolute HTTP or HTTPS URL.");
        RuleFor(request => request.MaterialType)
            .Must(CourseMaterialTypes.IsSupported)
            .WithMessage($"Material type must be one of: {string.Join(", ", CourseMaterialTypes.All)}.");
        RuleFor(request => request.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Visibility)
            .Must(CourseMaterialVisibilities.IsSupported)
            .WithMessage($"Visibility must be one of: {string.Join(", ", CourseMaterialVisibilities.All)}.");
    }

    private static bool IsHttpUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
