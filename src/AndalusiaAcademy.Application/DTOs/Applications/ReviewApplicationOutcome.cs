namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record ReviewApplicationOutcome(bool Succeeded, ApplicationDto? Application, string? Error)
{
    public static ReviewApplicationOutcome Success(ApplicationDto application) => new(true, application, null);

    public static ReviewApplicationOutcome Failure(string error) => new(false, null, error);
}
