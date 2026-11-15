namespace AndalusiaAcademy.Application.DTOs.Applications;

public sealed record CreateApplicationOutcome(bool Succeeded, ApplicationDto? Application, IReadOnlyList<string> Errors)
{
    public static CreateApplicationOutcome Success(ApplicationDto application) => new(true, application, []);

    public static CreateApplicationOutcome Failure(string error) => new(false, null, [error]);
}
