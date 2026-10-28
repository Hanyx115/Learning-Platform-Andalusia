using System.Text.RegularExpressions;
using AndalusiaAcademy.Application.DTOs.Cms;
using AndalusiaAcademy.Domain.Models;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Cms;

public sealed partial class UpsertCmsContentItemRequestValidator : AbstractValidator<UpsertCmsContentItemRequest>
{
    public UpsertCmsContentItemRequestValidator()
    {
        RuleFor(request => request.ContentType)
            .Must(type => CmsContentTypes.All.Contains(type?.Trim() ?? string.Empty))
            .WithMessage("Choose a supported content type.");
        RuleFor(request => request.Key)
            .NotEmpty()
            .MaximumLength(100)
            .Must(key => KeyPattern().IsMatch(key ?? string.Empty))
            .WithMessage("Key may contain lowercase letters, numbers, and hyphens only.");
        RuleFor(request => request.TitleEn).NotEmpty().MaximumLength(250);
        RuleFor(request => request.TitleAr).NotEmpty().MaximumLength(250);
        RuleFor(request => request.BodyEn).NotEmpty().MaximumLength(4000);
        RuleFor(request => request.BodyAr).NotEmpty().MaximumLength(4000);
        RuleFor(request => request.ImageUrl).MaximumLength(1000).Must(BeOptionalWebUrl);
        RuleFor(request => request.LinkUrl).MaximumLength(1000).Must(BeOptionalWebUrl);
        RuleFor(request => request.DisplayOrder).GreaterThanOrEqualTo(0);
    }

    private static bool BeOptionalWebUrl(string? value)
        => string.IsNullOrWhiteSpace(value) ||
           (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex KeyPattern();
}
