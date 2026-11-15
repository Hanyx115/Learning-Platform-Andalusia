namespace AndalusiaAcademy.Application.DTOs;

public sealed class ProgramCourseItemDto
{
    public Guid CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public string CourseSlug { get; init; } = string.Empty;

    public int SequenceOrder { get; init; }
}
