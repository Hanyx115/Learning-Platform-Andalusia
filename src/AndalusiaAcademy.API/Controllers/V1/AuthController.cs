using System.Security.Claims;
using AndalusiaAcademy.API.Configuration;
using AndalusiaAcademy.Application.DTOs.Auth;
using AndalusiaAcademy.Application.Identity;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<ForgotPasswordRequest> forgotPasswordValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator,
    IHostEnvironment hostEnvironment,
    IOptions<RefreshTokenCookieOptions> refreshCookieOptions) : ControllerBase
{
    private readonly RefreshTokenCookieOptions _refreshCookie = refreshCookieOptions.Value;

    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimitPolicies.Registration)]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResultDto>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await authService.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        if (!outcome.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(RegisterRequest.Email)] = outcome.Errors.ToArray(),
            }));
        }

        SetRefreshTokenCookie(outcome.Tokens!);
        return Ok(outcome.Tokens);
    }

    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimitPolicies.Login)]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        if (!outcome.Succeeded)
        {
            var message = outcome.FailureReason == LoginFailureReason.LockedOut
                ? "This account is temporarily locked due to repeated failed sign-in attempts."
                : "Invalid email or password.";

            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = message,
            });
        }

        SetRefreshTokenCookie(outcome.Tokens!);
        return Ok(outcome.Tokens);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(AuthRateLimitPolicies.Session)]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(_refreshCookie.Name, out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            return InvalidRefreshToken();
        }

        var outcome = await authService.RefreshAsync(
            new RefreshRequest(refreshToken),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!outcome.Succeeded)
        {
            DeleteRefreshTokenCookie();
            return InvalidRefreshToken();
        }

        SetRefreshTokenCookie(outcome.Tokens!);
        return Ok(outcome.Tokens);
    }

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting(AuthRateLimitPolicies.Session)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(_refreshCookie.Name, out var refreshToken)
            && !string.IsNullOrWhiteSpace(refreshToken))
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await authService.LogoutAsync(userId, refreshToken, cancellationToken);
        }

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(AuthRateLimitPolicies.Recovery)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await forgotPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await authService.ForgotPasswordAsync(request.Email, cancellationToken);

        // Never reveal whether the account exists. The reset token is only ever exposed here in
        // Development and isolated integration tests, since no email provider exists yet to deliver it.
        var exposeResetToken = hostEnvironment.IsDevelopment()
            || hostEnvironment.IsEnvironment("Testing");

        return Ok(new
        {
            message = "If that email is registered, a password reset link has been sent.",
            resetToken = exposeResetToken ? outcome.ResetToken : null,
        });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(AuthRateLimitPolicies.Recovery)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await resetPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await authService.ResetPasswordAsync(request, cancellationToken);

        if (!outcome.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(ResetPasswordRequest.Token)] = outcome.Errors.ToArray(),
            }));
        }

        DeleteRefreshTokenCookie();
        return Ok();
    }

    private UnauthorizedObjectResult InvalidRefreshToken() => Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Invalid or expired refresh token.",
    });

    private void SetRefreshTokenCookie(AuthResultDto tokens)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Cookies.Append(
            _refreshCookie.Name,
            tokens.RefreshToken,
            CreateRefreshCookieOptions(tokens.RefreshTokenExpiresAtUtc));
    }

    private void DeleteRefreshTokenCookie() => Response.Cookies.Delete(
        _refreshCookie.Name,
        CreateRefreshCookieOptions(expiresAtUtc: null));

    private CookieOptions CreateRefreshCookieOptions(DateTimeOffset? expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = _refreshCookie.Secure,
        SameSite = _refreshCookie.SameSite,
        IsEssential = true,
        Path = RefreshTokenCookieOptions.CookiePath,
        Expires = expiresAtUtc,
    };
}
