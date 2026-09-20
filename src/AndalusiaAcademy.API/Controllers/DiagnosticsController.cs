using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController : ControllerBase
{
    [HttpGet("throw")]
    public IActionResult Throw() =>
        throw new InvalidOperationException("Diagnostic endpoint used to verify global exception handling.");
}
