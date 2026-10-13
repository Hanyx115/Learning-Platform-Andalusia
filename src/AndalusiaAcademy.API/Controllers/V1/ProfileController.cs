using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Identity;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile")]
[Authorize]
public sealed class ProfileController(
    IProfileService profileService,
    IValidator<UpdateProfileRequest> updateProfileValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await profileService.GetProfileAsync(GetUserId(), cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        await updateProfileValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await profileService.UpdateProfileAsync(GetUserId(), request, cancellationToken);

        if (!outcome.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(UpdateProfileRequest.FirstName)] = outcome.Errors.ToArray(),
            }));
        }

        return Ok(outcome.Profile);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
