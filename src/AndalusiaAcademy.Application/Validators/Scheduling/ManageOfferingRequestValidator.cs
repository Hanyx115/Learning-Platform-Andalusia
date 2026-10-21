using AndalusiaAcademy.Application.DTOs.Scheduling;
using AndalusiaAcademy.Domain.Models;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Scheduling;

public sealed class ManageOfferingRequestValidator : AbstractValidator<ManageOfferingRequest>
{
    public ManageOfferingRequestValidator()
    {
        RuleFor(request => request.EndsAtUtc)
            .GreaterThan(request => request.StartsAtUtc)
            .WithMessage("The offering end time must be after its start time.");
        RuleFor(request => request.ApplicationClosesAtUtc)
            .GreaterThan(request => request.ApplicationOpensAtUtc)
            .LessThanOrEqualTo(request => request.StartsAtUtc);
        RuleFor(request => request.Price).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Currency).NotEmpty().Length(3);
        RuleFor(request => request.Capacity).GreaterThan(0);
        RuleFor(request => request.Status)
            .Must(status => status is OfferingStatuses.Draft
                or OfferingStatuses.Open
                or OfferingStatuses.Closed
                or OfferingStatuses.Cancelled)
            .WithMessage("Status must be Draft, Open, Closed, or Cancelled.");
    }
}
