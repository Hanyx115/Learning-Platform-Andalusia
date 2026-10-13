namespace AndalusiaAcademy.Application.DTOs.Auth;

public sealed record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
