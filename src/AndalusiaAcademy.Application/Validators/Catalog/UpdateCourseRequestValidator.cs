using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Domain.Models;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Catalog;

public sealed class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.ImageUrl).Must(CatalogImageUrlValidator.IsValid)
            .WithMessage("Choose an image uploaded through the catalog image service.");
        RuleFor(request => request.Status)
            .Must(status => status is CatalogStatuses.Draft or CatalogStatuses.Published or CatalogStatuses.Archived)
            .WithMessage("Status must be one of: Draft, Published, Archived.");
        RuleFor(request => request.Type)
            .Must(type => type is CourseTypes.Online or CourseTypes.Offline or CourseTypes.Hybrid)
            .WithMessage("Type must be one of: Online, Offline, Hybrid.");
        RuleFor(request => request.Price).GreaterThanOrEqualTo(0).When(request => request.Price.HasValue);
    }
}
