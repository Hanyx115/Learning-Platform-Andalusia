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

        var tokens = await IssueTokensAsync(user, ipAddress, cancellationToken);
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

        var tokens = await IssueTokensAsync(user, ipAddress, cancellationToken);
        return LoginOutcome.Success(tokens);
    }

    private async Task<AuthResultDto> IssueTokensAsync(
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

        return new AuthResultDto(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.PlainText,
            refreshToken.ExpiresAtUtc);
    }
}
