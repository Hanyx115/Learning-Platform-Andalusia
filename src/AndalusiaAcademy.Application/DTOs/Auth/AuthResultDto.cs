using System.Text.Json.Serialization;

namespace AndalusiaAcademy.Application.DTOs.Auth;

public sealed record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    [property: JsonIgnore]
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
