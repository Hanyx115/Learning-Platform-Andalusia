namespace AndalusiaAcademy.Application.DTOs.Materials;

public sealed record CourseMaterialFileDto(
    Stream Content,
    string ContentType,
    string OriginalFileName);

public sealed record CourseMaterialFileOutcome(
    bool Succeeded,
    CourseMaterialFileDto? Value,
    CourseMaterialFailureKind? FailureKind,
    string? Error)
{
    public static CourseMaterialFileOutcome Success(CourseMaterialFileDto value)
        => new(true, value, null, null);

    public static CourseMaterialFileOutcome Failure(CourseMaterialFailureKind failureKind, string error)
        => new(false, null, failureKind, error);
}
