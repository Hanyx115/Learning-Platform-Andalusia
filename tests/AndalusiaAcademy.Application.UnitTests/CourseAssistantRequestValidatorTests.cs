using AndalusiaAcademy.Application.DTOs.Assistant;
using AndalusiaAcademy.Application.Validators.Assistant;

namespace AndalusiaAcademy.Application.UnitTests;

public sealed class CourseAssistantRequestValidatorTests
{
    private readonly CourseAssistantRequestValidator _validator = new();

    [Fact]
    public void Validates_BilingualGroundedQuestion()
    {
        var request = new CourseAssistantRequest(
            "Explain the network model.",
            "en",
            [new CourseAssistantMessageDto("user", "What should I read first?")]);

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Rejects_EmptyQuestionAndUnsupportedLanguage()
    {
        var request = new CourseAssistantRequest("", "fr", []);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Question));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Language));
    }

    [Fact]
    public void Rejects_TooMuchOrUntrustedHistory()
    {
        var history = Enumerable.Range(0, 7)
            .Select(index => new CourseAssistantMessageDto(index == 0 ? "system" : "user", "Message"))
            .ToArray();
        var request = new CourseAssistantRequest("Question", "ar-EG", history);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.History));
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("user or assistant"));
    }
}
