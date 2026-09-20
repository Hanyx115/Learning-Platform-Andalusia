using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.IntegrationTests;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/test-errors")]
public sealed class ThrowingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        throw new InvalidOperationException("Test exception used to verify global handling.");
}
