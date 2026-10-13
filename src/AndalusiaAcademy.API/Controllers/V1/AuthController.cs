using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Auth;
using AndalusiaAcademy.Application.Identity;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<LogoutRequest> logoutValidator,
    IValidator<ForgotPasswordRequest> forgotPasswordValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpPost("register")]
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

        return Ok(outcome.Tokens);
    }

    [HttpPost("login")]
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

        return Ok(outcome.Tokens);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await logoutValidator.ValidateAndThrowAsync(request, cancellationToken);

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await authService.LogoutAsync(userId, request.RefreshToken, cancellationToken);

        return NoContent();
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await forgotPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await authService.ForgotPasswordAsync(request.Email, cancellationToken);

        // Never reveal whether the account exists. The reset token is only ever exposed here in
        // Development, since no email provider exists yet to deliver it (tracked for a later sprint).
        return Ok(new
        {
            message = "If that email is registered, a password reset link has been sent.",
            resetToken = hostEnvironment.IsDevelopment() ? outcome.ResetToken : null,
        });
    }

    [HttpPost("reset-password")]
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

        return Ok();
    }
}
