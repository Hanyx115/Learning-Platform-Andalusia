using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Domain.Models;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Catalog;

public sealed class CareerPathRequestValidator : AbstractValidator<CareerPathRequest>
{
    public CareerPathRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.RecommendedSkills).NotEmpty().MaximumLength(1000);
        RuleFor(request => request.Status)
            .Must(status => status is CatalogStatuses.Draft or CatalogStatuses.Published or CatalogStatuses.Archived)
            .WithMessage("Status must be one of: Draft, Published, Archived.");
        RuleFor(request => request.RecommendedProgramIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Recommended programs cannot contain duplicates.");
        RuleFor(request => request.RecommendedCourseIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Recommended courses cannot contain duplicates.");
    }
}
