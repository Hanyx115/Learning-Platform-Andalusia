using AndalusiaAcademy.Application.DTOs;

namespace AndalusiaAcademy.Application.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        string? createdByIp,
        CancellationToken cancellationToken);

    Task<RefreshTokenDto?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the token revoked and rotates it to <paramref name="replacedByTokenHash"/> when provided,
    /// so a reused (already-rotated) refresh token can be detected and the whole chain invalidated.
    /// </summary>
    Task RevokeAsync(
        string tokenHash,
        string? replacedByTokenHash,
        string? revokedByIp,
        CancellationToken cancellationToken);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}
