namespace AndalusiaAcademy.Application.DTOs.Operations;

public sealed record OperationsOverviewDto(
    int ActiveStudents,
    int ActiveInstructors,
    int RunningCourses,
    int PendingApplications,
    int ActiveEnrollments,
    IReadOnlyList<RevenueSummaryDto> Revenue,
    IReadOnlyList<StatusSummaryDto> ApplicationsByStatus,
    IReadOnlyList<StatusSummaryDto> PaymentsByStatus,
    IReadOnlyList<StatusSummaryDto> EnrollmentsByStatus,
    IReadOnlyList<OperationsActivityDto> RecentActivity);

public sealed record RevenueSummaryDto(string Currency, decimal Amount, int PaymentCount);

public sealed record StatusSummaryDto(string Status, int Count);

public sealed record OperationsActivityDto(
    Guid Id,
    string ActivityType,
    string Status,
    DateTimeOffset OccurredAtUtc,
    string LearnerName,
    string OfferingTitle);
