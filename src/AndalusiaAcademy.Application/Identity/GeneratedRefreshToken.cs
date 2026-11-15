namespace AndalusiaAcademy.Application.Identity;

/// <summary>
/// <see cref="PlainText"/> is returned to the client once and never persisted;
/// only <see cref="Hash"/> is stored so a leaked database cannot be used to replay refresh tokens.
/// </summary>
public sealed record GeneratedRefreshToken(string PlainText, string Hash, DateTimeOffset ExpiresAtUtc);
