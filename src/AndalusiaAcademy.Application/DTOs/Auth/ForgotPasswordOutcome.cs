namespace AndalusiaAcademy.Application.DTOs.Auth;

/// <summary>
/// <see cref="ResetToken"/> is populated only when the account exists; callers decide whether it's safe
/// to expose (e.g. only in Development, until a real email provider delivers it instead).
/// </summary>
public sealed record ForgotPasswordOutcome(string? ResetToken);
