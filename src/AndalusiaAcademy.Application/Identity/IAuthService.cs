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

    Task<ForgotPasswordOutcome> ForgotPasswordAsync(string email, CancellationToken cancellationToken);

    Task<ResetPasswordOutcome> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
