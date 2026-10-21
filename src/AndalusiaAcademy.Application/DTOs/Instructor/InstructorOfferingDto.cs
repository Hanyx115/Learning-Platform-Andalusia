namespace AndalusiaAcademy.Application.DTOs.Instructor;

public sealed record InstructorOfferingDto(
    Guid? CourseOfferingId,
    Guid? ProgramOfferingId,
    string OfferingType,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    int EnrolledStudentCount);
