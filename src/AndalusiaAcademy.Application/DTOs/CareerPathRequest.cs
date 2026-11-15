namespace AndalusiaAcademy.Application.DTOs;

public sealed record CareerPathRequest(
    string Title,
    string Description,
    string RecommendedSkills,
    string Status,
    IReadOnlyCollection<Guid>? RecommendedProgramIds,
    IReadOnlyCollection<Guid>? RecommendedCourseIds,
    string? ImageUrl = null);
