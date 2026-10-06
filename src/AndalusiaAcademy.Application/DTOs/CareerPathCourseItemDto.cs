namespace AndalusiaAcademy.Application.DTOs;

public sealed class CareerPathCourseItemDto
{
    public Guid CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public string CourseSlug { get; init; } = string.Empty;
}
