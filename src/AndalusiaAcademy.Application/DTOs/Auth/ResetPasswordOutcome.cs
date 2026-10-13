namespace AndalusiaAcademy.Application.DTOs.Auth;

public sealed record ResetPasswordOutcome(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static ResetPasswordOutcome Success() => new(true, []);

    public static ResetPasswordOutcome Failure(IReadOnlyList<string> errors) => new(false, errors);
}
