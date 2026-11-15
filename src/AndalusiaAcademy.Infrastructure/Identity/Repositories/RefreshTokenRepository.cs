using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Identity.Repositories;

public sealed class RefreshTokenRepository(ApplicationDbContext dbContext) : IRefreshTokenRepository
{
    public async Task AddAsync(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        string? createdByIp,
        CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByIp = createdByIp,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshTokenDto?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);

        return token is null
            ? null
            : new RefreshTokenDto(token.Id, token.UserId, token.TokenHash, token.ExpiresAtUtc, token.RevokedAtUtc);
    }

    public async Task RevokeAsync(
        string tokenHash,
        string? replacedByTokenHash,
        string? revokedByIp,
        CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAtUtc is not null)
        {
            return;
        }

        token.RevokedAtUtc = DateTimeOffset.UtcNow;
        token.ReplacedByTokenHash = replacedByTokenHash;
        token.RevokedByIp = revokedByIp;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == userId && refreshToken.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAtUtc, DateTimeOffset.UtcNow),
                cancellationToken);
    }
}
