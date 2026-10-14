using AndalusiaAcademy.Application.DTOs.Payments;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Payments;

public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(request => request.ApplicationId)
            .NotEmpty();
    }
}
