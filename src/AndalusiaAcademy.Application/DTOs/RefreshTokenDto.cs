namespace AndalusiaAcademy.Application.DTOs;

public sealed record RefreshTokenDto(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc);
