namespace AndalusiaAcademy.Application.DTOs;

public sealed class CareerPathSummaryDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}
