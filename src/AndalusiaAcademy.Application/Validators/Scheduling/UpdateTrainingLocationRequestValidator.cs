using AndalusiaAcademy.Application.DTOs.Scheduling;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Scheduling;

public sealed class UpdateTrainingLocationRequestValidator : AbstractValidator<UpdateTrainingLocationRequest>
{
    public UpdateTrainingLocationRequestValidator()
    {
        RuleFor(request => request.BranchName).NotEmpty().MaximumLength(150);
        RuleFor(request => request.RoomName).NotEmpty().MaximumLength(100);
        RuleFor(request => request.City).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Address).NotEmpty().MaximumLength(500);
        RuleFor(request => request.MapUrl)
            .MaximumLength(500)
            .Must(value => string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out _))
            .WithMessage("Map URL must be an absolute URL.");
    }
}
