using AndalusiaAcademy.Application.DTOs.Auth;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        // Password strength itself is enforced by ASP.NET Identity's configured policy at creation time.
        RuleFor(request => request.Password)
            .NotEmpty();

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
