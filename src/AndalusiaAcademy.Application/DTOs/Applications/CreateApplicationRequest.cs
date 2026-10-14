namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record CreateApplicationRequest(Guid? CourseId, Guid? ProgramId);
