using AndalusiaAcademy.Application.DTOs;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(request => request.CountryCode)
            .NotEmpty()
            .MaximumLength(2);

        RuleFor(request => request.PreferredLanguage)
            .NotEmpty()
            .MaximumLength(5);
    }
}
