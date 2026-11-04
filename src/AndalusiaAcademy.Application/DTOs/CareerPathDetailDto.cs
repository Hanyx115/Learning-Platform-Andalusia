namespace AndalusiaAcademy.Application.DTOs;

public sealed class CareerPathDetailDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string RecommendedSkills { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public IReadOnlyList<CareerPathProgramItemDto> RecommendedPrograms { get; init; } = [];

    public IReadOnlyList<CareerPathCourseItemDto> RecommendedCourses { get; init; } = [];
}
