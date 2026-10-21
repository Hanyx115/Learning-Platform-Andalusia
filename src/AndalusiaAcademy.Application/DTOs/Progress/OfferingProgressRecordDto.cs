namespace AndalusiaAcademy.Application.DTOs.Progress;

/// <summary>The staff/instructor roster view of progress for one learner within a single offering.</summary>
public sealed record OfferingProgressRecordDto(
    Guid UserId,
    string LearnerName,
    string Email,
    int TotalSessions,
    int AttendedSessions,
    decimal AttendanceRatePercentage,
    int TotalAssignments,
    int SubmittedAssignments,
    int GradedAssignments,
    decimal? AverageScorePercentage,
    decimal OverallProgressPercentage);
