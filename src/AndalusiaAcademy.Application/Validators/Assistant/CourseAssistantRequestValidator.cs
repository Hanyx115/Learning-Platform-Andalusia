using AndalusiaAcademy.Application.DTOs.Assistant;
using FluentValidation;

namespace AndalusiaAcademy.Application.Validators.Assistant;

public sealed class CourseAssistantRequestValidator : AbstractValidator<CourseAssistantRequest>
{
    public CourseAssistantRequestValidator()
    {
        RuleFor(request => request.Question)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(request => request.Language)
            .Must(language => string.IsNullOrWhiteSpace(language) ||
                language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ||
                language.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Language must be English or Arabic.");

        RuleFor(request => request.History)
            .Must(history => history is null || history.Count <= 6)
            .WithMessage("A maximum of 6 previous messages can be included.");

        RuleForEach(request => request.History)
            .ChildRules(message =>
            {
                message.RuleFor(item => item.Role)
                    .Must(role => role is "user" or "assistant")
                    .WithMessage("Message role must be user or assistant.");
                message.RuleFor(item => item.Content)
                    .NotEmpty()
                    .MaximumLength(2000);
            });
    }
}
