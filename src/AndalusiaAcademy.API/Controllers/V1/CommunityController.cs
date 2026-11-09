using System.Security.Claims;
using AndalusiaAcademy.Application.DTOs.Community;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/community")]
[Authorize]
public sealed class CommunityController(CommunityService service) : ControllerBase
{
    [HttpGet("{offeringType}/{offeringId:guid}")]
    public async Task<ActionResult<CommunityPageDto>> GetPage(
        string offeringType, Guid offeringId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var scope = ResolveScope(offeringType, offeringId);
        if (scope is null)
        {
            return BadRequest(new ProblemDetails { Title = "Offering type must be course or program." });
        }

        return Map(await service.GetPageAsync(
            GetUserId(), scope.Value.CourseOfferingId, scope.Value.ProgramOfferingId,
            IsPrivileged(), page, pageSize, cancellationToken));
    }

    [HttpPost("{offeringType}/{offeringId:guid}/posts")]
    public async Task<ActionResult<CommunityPostDto>> CreatePost(
        string offeringType, Guid offeringId, CreateCommunityPostRequest request,
        CancellationToken cancellationToken)
    {
        var scope = ResolveScope(offeringType, offeringId);
        if (scope is null)
        {
            return BadRequest(new ProblemDetails { Title = "Offering type must be course or program." });
        }

        var outcome = await service.CreatePostAsync(
            GetUserId(), scope.Value.CourseOfferingId, scope.Value.ProgramOfferingId,
            IsPrivileged(), request, cancellationToken);
        return MapCreated(outcome);
    }

    [HttpPost("posts/{postId:guid}/replies")]
    public async Task<ActionResult<CommunityReplyDto>> CreateReply(
        Guid postId, CreateCommunityReplyRequest request, CancellationToken cancellationToken)
        => MapCreated(await service.CreateReplyAsync(GetUserId(), postId, IsPrivileged(), request, cancellationToken));

    [HttpPatch("posts/{postId:guid}/moderation")]
    public async Task<ActionResult<CommunityPostDto>> Moderate(
        Guid postId, ModerateCommunityPostRequest request, CancellationToken cancellationToken)
        => Map(await service.ModerateAsync(GetUserId(), postId, IsPrivileged(), request, cancellationToken));

    private ActionResult<T> Map<T>(CommunityOutcome<T> outcome)
        => outcome.Succeeded && outcome.Value is not null
            ? Ok(outcome.Value)
            : MapFailure<T>(outcome);

    private ActionResult<T> MapCreated<T>(CommunityOutcome<T> outcome)
        => outcome.Succeeded && outcome.Value is not null
            ? StatusCode(StatusCodes.Status201Created, outcome.Value)
            : MapFailure<T>(outcome);

    private ActionResult<T> MapFailure<T>(CommunityOutcome<T> outcome)
    {
        var problem = new ProblemDetails { Title = outcome.Error };
        return outcome.FailureKind switch
        {
            CommunityFailureKind.NotFound => NotFound(problem),
            CommunityFailureKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, problem),
            CommunityFailureKind.Conflict => Conflict(problem),
            _ => BadRequest(problem),
        };
    }

    private bool IsPrivileged() => User.IsInRole(RoleNames.Manager) || User.IsInRole(RoleNames.Admin);

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static (Guid? CourseOfferingId, Guid? ProgramOfferingId)? ResolveScope(string offeringType, Guid offeringId)
        => offeringType.Trim().ToLowerInvariant() switch
        {
            "course" => (offeringId, null),
            "program" => (null, offeringId),
            _ => null,
        };
}
