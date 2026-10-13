using AndalusiaAcademy.Application.DTOs.Auth;

namespace AndalusiaAcademy.Application.Identity;

public interface IAuthService
{
    Task<RegisterOutcome> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<LoginOutcome> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    /// <summary>No-op (never throws) when the token doesn't exist or doesn't belong to <paramref name="userId"/>, so logout stays idempotent and doesn't leak which case occurred.</summary>
    Task LogoutAsync(Guid userId, string refreshToken, CancellationToken cancellationToken);

    /// <summary>Rotates the refresh token: the presented one is revoked and a new access/refresh pair is issued. Reusing an already-revoked token revokes the whole chain for that user (theft detection).</summary>
    Task<RefreshOutcome> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<ForgotPasswordOutcome> ForgotPasswordAsync(string email, CancellationToken cancellationToken);

    Task<ResetPasswordOutcome> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
