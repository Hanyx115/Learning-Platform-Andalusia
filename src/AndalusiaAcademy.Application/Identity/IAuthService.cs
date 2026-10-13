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
}
