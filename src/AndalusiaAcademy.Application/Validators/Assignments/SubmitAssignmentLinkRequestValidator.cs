using AndalusiaAcademy.Application.DTOs.Assignments;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Assignments;

public sealed class SubmitAssignmentLinkRequestValidator : AbstractValidator<SubmitAssignmentLinkRequest>
{
    public SubmitAssignmentLinkRequestValidator()
    {
        RuleFor(request => request.Url)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(BeAnHttpUrl)
            .WithMessage("Enter a valid http or https link.");
    }

    private static bool BeAnHttpUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
