namespace AndalusiaAcademy.Application.DTOs.Instructor;

public sealed record InstructorStudentDto(
    Guid UserId,
    string LearnerName,
    string Email,
    Guid? CourseOfferingId,
    Guid? ProgramOfferingId,
    string OfferingType,
    string OfferingTitle);
