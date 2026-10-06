namespace AndalusiaAcademy.Application.DTOs;

public sealed class CareerPathProgramItemDto
{
    public Guid ProgramId { get; init; }

    public string ProgramTitle { get; init; } = string.Empty;

    public string ProgramSlug { get; init; } = string.Empty;
}
