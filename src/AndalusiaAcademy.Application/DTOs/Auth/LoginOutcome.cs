namespace AndalusiaAcademy.Application.DTOs.Auth;

public sealed record LoginOutcome(bool Succeeded, AuthResultDto? Tokens, LoginFailureReason? FailureReason)
{
    public static LoginOutcome Success(AuthResultDto tokens) => new(true, tokens, null);

    public static LoginOutcome Failure(LoginFailureReason reason) => new(false, null, reason);
}
