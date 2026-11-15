namespace AndalusiaAcademy.Application.DTOs.Instructor;

public sealed record InstructorOverviewDto(
    int AssignedOfferings,
    int ActiveStudents,
    int UpcomingSessions,
    int PendingSubmissions,
    IReadOnlyList<InstructorSessionDto> NextSessions);
