using AndalusiaAcademy.Application.DTOs.Auth;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Auth;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Token)
            .NotEmpty();

        // Strength itself is enforced by ASP.NET Identity's configured policy at reset time.
        RuleFor(request => request.NewPassword)
            .NotEmpty();
    }
}
