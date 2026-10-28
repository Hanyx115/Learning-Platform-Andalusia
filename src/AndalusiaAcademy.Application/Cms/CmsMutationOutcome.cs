namespace AndalusiaAcademy.Application.Cms;

public sealed record CmsMutationOutcome<T>(bool Succeeded, T? Value, string? Error)
    where T : class
{
    public static CmsMutationOutcome<T> Success(T value) => new(true, value, null);

    public static CmsMutationOutcome<T> Failure(string error) => new(false, null, error);
}
