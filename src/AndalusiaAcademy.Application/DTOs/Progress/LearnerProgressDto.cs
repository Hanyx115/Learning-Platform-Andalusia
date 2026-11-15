namespace AndalusiaAcademy.Application.DTOs.Progress;

/// <summary>
/// Computed on read from Attendance + Assignments, never stored, so it can never go stale.
/// AttendanceRatePercentage/AssignmentSubmissionRatePercentage are the two "did the learner show
/// up and turn things in" signals; OverallProgressPercentage is their simple average. Scores
/// (AverageScorePercentage) reflect quality, not completion, so they are reported separately and
/// deliberately excluded from the progress figure itself.
/// </summary>
public sealed record LearnerProgressDto(
    Guid EnrollmentId,
    Guid? CourseOfferingId,
    string? CourseTitle,
    Guid? ProgramOfferingId,
    string? ProgramTitle,
    int TotalSessions,
    int AttendedSessions,
    decimal AttendanceRatePercentage,
    int TotalAssignments,
    int SubmittedAssignments,
    int GradedAssignments,
    decimal? AverageScorePercentage,
    decimal OverallProgressPercentage);
