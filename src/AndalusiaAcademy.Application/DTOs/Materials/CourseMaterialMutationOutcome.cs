namespace AndalusiaAcademy.Application.DTOs.Materials;

public enum CourseMaterialFailureKind
{
    NotFound,
    Validation,
}

public sealed record CourseMaterialMutationOutcome(
    bool Succeeded,
    CourseMaterialDto? Value,
    CourseMaterialFailureKind? FailureKind,
    string? Error)
{
    public static CourseMaterialMutationOutcome Success(CourseMaterialDto value)
        => new(true, value, null, null);

    public static CourseMaterialMutationOutcome Failure(
        CourseMaterialFailureKind failureKind,
        string error)
        => new(false, null, failureKind, error);
}
