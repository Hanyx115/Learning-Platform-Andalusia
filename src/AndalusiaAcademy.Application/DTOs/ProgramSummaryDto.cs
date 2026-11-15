namespace AndalusiaAcademy.Application.DTOs;

public sealed class ProgramSummaryDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public decimal? Price { get; init; }

    public string? ImageUrl { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
