namespace AndalusiaAcademy.Application.DTOs.Assistant;

public sealed record CourseAssistantMessageDto(string Role, string Content);

public sealed record CourseAssistantRequest(
    string Question,
    string? Language,
    IReadOnlyList<CourseAssistantMessageDto>? History);

public sealed record CourseAssistantCitationDto(
    Guid MaterialId,
    string Title,
    string ResourceUrl,
    string MaterialType);

public sealed record CourseAssistantResponseDto(
    string Answer,
    IReadOnlyList<CourseAssistantCitationDto> Citations,
    bool IsDemoMode);

public enum CourseAssistantFailureKind
{
    NotFound,
    Unavailable,
}

public sealed record CourseAssistantOutcome(
    bool Succeeded,
    CourseAssistantResponseDto? Value,
    CourseAssistantFailureKind? FailureKind,
    string? Error)
{
    public static CourseAssistantOutcome Success(CourseAssistantResponseDto value)
        => new(true, value, null, null);

    public static CourseAssistantOutcome Failure(CourseAssistantFailureKind kind, string error)
        => new(false, null, kind, error);
}
