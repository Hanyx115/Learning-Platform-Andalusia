using AndalusiaAcademy.Application.DTOs.Scheduling;

namespace AndalusiaAcademy.Application.DTOs.Instructor;

/// <summary>The only way an instructor discovers a session's id, which the existing
/// attendance-marking endpoints (course-sessions/{id}/attendance) require as input.</summary>
public sealed record InstructorSessionDto(
    Guid SessionId,
    Guid? CourseOfferingId,
    Guid? ProgramOfferingId,
    string OfferingType,
    string OfferingTitle,
    string SessionTitle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    TrainingLocationDto? Location);
