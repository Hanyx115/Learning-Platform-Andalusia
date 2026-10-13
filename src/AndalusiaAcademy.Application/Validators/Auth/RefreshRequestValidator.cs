using AndalusiaAcademy.Application.DTOs.Auth;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Auth;

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
