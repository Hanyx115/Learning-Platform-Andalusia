using AndalusiaAcademy.Application.DTOs.Auth;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace AndalusiaAcademy.Infrastructure.Identity.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository) : IAuthService
{
    public async Task<RegisterOutcome> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CountryCode = request.CountryCode,
            PreferredLanguage = request.PreferredLanguage,
            ConsentedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return RegisterOutcome.Failure(createResult.Errors.Select(error => error.Description).ToArray());
        }

        await userManager.AddToRoleAsync(user, RoleNames.Learner);

        var (tokens, _) = await IssueTokensAsync(user, ipAddress, cancellationToken);
        return RegisterOutcome.Success(tokens);
    }

    public async Task<LoginOutcome> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return LoginOutcome.Failure(LoginFailureReason.InvalidCredentials);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return LoginOutcome.Failure(LoginFailureReason.LockedOut);
        }

        if (!signInResult.Succeeded)
        {
            return LoginOutcome.Failure(LoginFailureReason.InvalidCredentials);
        }

        var (tokens, _) = await IssueTokensAsync(user, ipAddress, cancellationToken);
        return LoginOutcome.Success(tokens);
    }

    public async Task<RefreshOutcome> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var oldHash = tokenService.Hash(request.RefreshToken);
        var stored = await refreshTokenRepository.GetActiveByHashAsync(oldHash, cancellationToken);

        if (stored is null)
        {
            return RefreshOutcome.Failure();
        }

        if (stored.RevokedAtUtc is not null)
        {
            // A revoked token being presented again means it was stolen and used after the legitimate
            // client rotated past it; kill every session for this user rather than trust the chain further.
            await refreshTokenRepository.RevokeAllForUserAsync(stored.UserId, cancellationToken);
            return RefreshOutcome.Failure();
        }

        if (stored.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return RefreshOutcome.Failure();
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());

        if (user is null)
        {
            return RefreshOutcome.Failure();
        }

        var (tokens, newHash) = await IssueTokensAsync(user, ipAddress, cancellationToken);
        await refreshTokenRepository.RevokeAsync(oldHash, newHash, ipAddress, cancellationToken);

        return RefreshOutcome.Success(tokens);
    }

    public async Task LogoutAsync(Guid userId, string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.Hash(refreshToken);
        var stored = await refreshTokenRepository.GetActiveByHashAsync(tokenHash, cancellationToken);

        if (stored is null || stored.UserId != userId || stored.RevokedAtUtc is not null)
        {
            return;
        }

        await refreshTokenRepository.RevokeAsync(tokenHash, replacedByTokenHash: null, revokedByIp: null, cancellationToken);
    }

    public async Task<ForgotPasswordOutcome> ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return new ForgotPasswordOutcome(null);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return new ForgotPasswordOutcome(token);
    }

    public async Task<ResetPasswordOutcome> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            // Same generic failure as an invalid token, so this endpoint can't be used to enumerate accounts.
            return ResetPasswordOutcome.Failure(["Invalid or expired reset token."]);
        }

        var resetResult = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            return ResetPasswordOutcome.Failure(resetResult.Errors.Select(error => error.Description).ToArray());
        }

        // Invalidate every existing session: a password reset should force re-authentication everywhere.
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        return ResetPasswordOutcome.Success();
    }

    private async Task<(AuthResultDto Tokens, string RefreshTokenHash)> IssueTokensAsync(
        ApplicationUser user,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(new TokenClaimsUser(user.Id, user.Email!, roles.ToArray()));
        var refreshToken = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(
            user.Id,
            refreshToken.Hash,
            refreshToken.ExpiresAtUtc,
            ipAddress,
            cancellationToken);

        var tokens = new AuthResultDto(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.PlainText,
            refreshToken.ExpiresAtUtc);

        return (tokens, refreshToken.Hash);
    }
}
