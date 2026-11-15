using AndalusiaAcademy.Application.DTOs.Notifications;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Notifications;

public sealed class UpdateNotificationTemplateRequestValidator : AbstractValidator<UpdateNotificationTemplateRequest>
{
    public UpdateNotificationTemplateRequestValidator()
    {
        RuleFor(request => request.TitleEn).NotEmpty().MaximumLength(250);
        RuleFor(request => request.TitleAr).NotEmpty().MaximumLength(250);
        RuleFor(request => request.MessageEn).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.MessageAr).NotEmpty().MaximumLength(2000);
    }
}
