using AndalusiaAcademy.Application.DTOs;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalog-images")]
public sealed class CatalogImagesController(CatalogImageService imageService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PolicyNames.CatalogEditor)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType<CatalogImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogImageDto>> Upload(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["file"] = ["Choose an image to upload."],
            }));
        }

        await using var content = file.OpenReadStream();
        var outcome = await imageService.UploadAsync(
            content,
            file.FileName,
            file.Length,
            cancellationToken);

        return outcome.Succeeded
            ? Created(outcome.Value!.ImageUrl, outcome.Value)
            : ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["file"] = [outcome.Error ?? "The catalog image is invalid."],
            }));
    }

    [HttpGet("{storedFileName}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string storedFileName, CancellationToken cancellationToken)
    {
        var image = await imageService.OpenAsync(storedFileName, cancellationToken);
        return image is null ? NotFound() : File(image.Content, image.ContentType);
    }
}
