using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Payments;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
[Authorize]
public sealed class PaymentsController(
    PaymentService paymentService,
    IValidator<CreatePaymentRequest> createValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireLearner)]
    [ProducesResponseType<PayForApplicationOutcome>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PayForApplicationOutcome>> Pay(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var outcome = await paymentService.PayAsync(request.ApplicationId, GetUserId(), cancellationToken);

        if (!outcome.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = [outcome.Error ?? "Payment failed."],
            }));
        }

        return Ok(outcome);
    }

    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyList<PaymentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetMine(CancellationToken cancellationToken)
    {
        var payments = await paymentService.GetMyPaymentsAsync(GetUserId(), cancellationToken);
        return Ok(payments);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.RequireManagerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<AdminPaymentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminPaymentDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await paymentService.GetAllAsync(cancellationToken));

    [HttpPost("{paymentId:guid}/fail")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    [ProducesResponseType<AdminPaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminPaymentDto>> FailPending(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var outcome = await paymentService.FailPendingAsync(paymentId, GetUserId(), cancellationToken);
        if (outcome.Succeeded)
        {
            return Ok(outcome.Payment);
        }

        var problem = new ProblemDetails { Title = outcome.Error };
        if (outcome.Failure == AdminPaymentActionFailure.NotFound)
        {
            problem.Status = StatusCodes.Status404NotFound;
            return NotFound(problem);
        }

        problem.Status = StatusCodes.Status409Conflict;
        return Conflict(problem);
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
