using AndalusiaAcademy.Application.DTOs.Auth;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Auth;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
