namespace AndalusiaAcademy.Application.DTOs.Auth;

public sealed record RegisterOutcome(bool Succeeded, AuthResultDto? Tokens, IReadOnlyList<string> Errors)
{
    public static RegisterOutcome Success(AuthResultDto tokens) => new(true, tokens, []);

    public static RegisterOutcome Failure(IReadOnlyList<string> errors) => new(false, null, errors);
}
