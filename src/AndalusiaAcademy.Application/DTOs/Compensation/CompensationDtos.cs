namespace AndalusiaAcademy.Application.DTOs.Compensation;

public sealed record InstructorCompensationTargetDto(
    Guid? RateId,
    Guid InstructorUserId,
    string InstructorName,
    Guid CourseOfferingId,
    string CourseTitle,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string OfferingStatus,
    decimal? Amount,
    string Currency,
    bool IsEligibleForPayout,
    string? PayoutStatus);

public sealed record StaffCompensationRateDto(
    Guid UserId,
    string StaffName,
    string Role,
    decimal? MonthlyAmount,
    string Currency);

public sealed record CompensationPayoutDto(
    Guid Id,
    Guid UserId,
    string PayeeName,
    string PayoutType,
    string Description,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    DateTimeOffset? ApprovedAtUtc);

public sealed record SetInstructorCourseRateRequest(
    Guid InstructorUserId,
    Guid CourseOfferingId,
    decimal Amount,
    string Currency);

public sealed record SetStaffMonthlyRateRequest(decimal Amount, string Currency);

public sealed record CreateStaffMonthlyPayoutRequest(int Year, int Month);

public sealed record CompensationActionOutcome(bool Succeeded, string? Error, CompensationPayoutDto? Payout)
{
    public static CompensationActionOutcome Success(CompensationPayoutDto payout) => new(true, null, payout);

    public static CompensationActionOutcome Failure(string error) => new(false, error, null);
}
