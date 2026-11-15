namespace AndalusiaAcademy.Application.DTOs.Auth;

public sealed record RefreshOutcome(bool Succeeded, AuthResultDto? Tokens)
{
    public static RefreshOutcome Success(AuthResultDto tokens) => new(true, tokens);

    public static RefreshOutcome Failure() => new(false, null);
}
