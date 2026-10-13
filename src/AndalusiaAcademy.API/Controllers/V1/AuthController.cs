using AndalusiaAcademy.Application.DTOs.Auth;
using AndalusiaAcademy.Application.Identity;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : ControllerBase
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
}
