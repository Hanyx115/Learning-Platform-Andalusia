namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record CreateApplicationRequest(Guid? CourseOfferingId, Guid? ProgramOfferingId);
