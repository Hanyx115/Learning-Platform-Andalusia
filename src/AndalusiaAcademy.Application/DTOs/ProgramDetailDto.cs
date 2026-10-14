namespace AndalusiaAcademy.Application.DTOs;

public sealed class ProgramDetailDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public decimal? Price { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public IReadOnlyList<ProgramCourseItemDto> Courses { get; init; } = [];
}
