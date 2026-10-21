namespace AndalusiaAcademy.Application.DTOs.Scheduling;

public enum SchedulingFailureKind
{
    Validation,
    NotFound,
    Conflict,
}

public sealed record SchedulingMutationOutcome<T>(
    bool Succeeded,
    T? Value,
    SchedulingFailureKind? FailureKind,
    string? Error)
    where T : class
{
    public static SchedulingMutationOutcome<T> Success(T value) => new(true, value, null, null);

    public static SchedulingMutationOutcome<T> Failure(SchedulingFailureKind kind, string error)
        => new(false, null, kind, error);
}
