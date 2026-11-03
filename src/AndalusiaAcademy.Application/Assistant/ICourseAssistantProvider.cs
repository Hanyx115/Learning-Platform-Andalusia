using AndalusiaAcademy.Application.DTOs.Assistant;

namespace AndalusiaAcademy.Application.Assistant;

public sealed record CourseAssistantSource(
    Guid MaterialId,
    string Title,
    string? Description,
    string ResourceUrl,
    string MaterialType);

public sealed record CourseAssistantProviderRequest(
    string Question,
    string Language,
    IReadOnlyList<CourseAssistantMessageDto> History,
    IReadOnlyList<CourseAssistantSource> Sources);

public sealed record CourseAssistantProviderResult(
    bool Succeeded,
    string? Answer,
    bool IsDemoMode,
    string? Error)
{
    public static CourseAssistantProviderResult Success(string answer, bool isDemoMode)
        => new(true, answer, isDemoMode, null);

    public static CourseAssistantProviderResult Failure(string error)
        => new(false, null, false, error);
}

public interface ICourseAssistantProvider
{
    Task<CourseAssistantProviderResult> AskAsync(
        CourseAssistantProviderRequest request,
        CancellationToken cancellationToken);
}
