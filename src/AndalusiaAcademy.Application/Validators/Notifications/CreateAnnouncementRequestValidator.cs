using AndalusiaAcademy.Application.DTOs.Notifications;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Notifications;

public sealed class CreateAnnouncementRequestValidator : AbstractValidator<CreateAnnouncementRequest>
{
    public CreateAnnouncementRequestValidator()
    {
        RuleFor(request => request.RecipientUserIds).NotEmpty();
        RuleForEach(request => request.RecipientUserIds).NotEqual(Guid.Empty);
        RuleFor(request => request.TitleEn).NotEmpty().MaximumLength(250);
        RuleFor(request => request.TitleAr).NotEmpty().MaximumLength(250);
        RuleFor(request => request.MessageEn).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.MessageAr).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.LinkUrl).MaximumLength(1000).Must(BeOptionalWebUrl);
    }

    private static bool BeOptionalWebUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        var isInternalPath = trimmed.StartsWith("/", StringComparison.Ordinal) &&
                             !trimmed.StartsWith("//", StringComparison.Ordinal) &&
                             Uri.TryCreate(trimmed, UriKind.Relative, out _);
        var isWebUrl = Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
                       (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        return isInternalPath || isWebUrl;
    }
}
