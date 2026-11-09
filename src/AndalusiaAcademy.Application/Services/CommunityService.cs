using AndalusiaAcademy.Application.DTOs.Community;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Services;

public sealed class CommunityService(ICommunityRepository repository, TimeProvider timeProvider)
{
    public async Task<CommunityOutcome<CommunityPageDto>> GetPageAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, bool isPrivileged,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var scopeError = ValidateOfferingScope(courseOfferingId, programOfferingId);
        if (scopeError is not null)
        {
            return CommunityOutcome<CommunityPageDto>.Failure(CommunityFailureKind.Validation, scopeError);
        }

        var access = await repository.GetAccessAsync(userId, courseOfferingId, programOfferingId, isPrivileged, cancellationToken);
        if (!access.OfferingExists || !access.CanRead)
        {
            return CommunityOutcome<CommunityPageDto>.Failure(CommunityFailureKind.NotFound, "Community not found.");
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 50);
        return CommunityOutcome<CommunityPageDto>.Success(await repository.GetPageAsync(
            userId, courseOfferingId, programOfferingId, access, safePage, safePageSize, cancellationToken));
    }

    public async Task<CommunityOutcome<CommunityPostDto>> CreatePostAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, bool isPrivileged,
        CreateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        var scopeError = ValidateOfferingScope(courseOfferingId, programOfferingId);
        if (scopeError is not null)
        {
            return CommunityOutcome<CommunityPostDto>.Failure(CommunityFailureKind.Validation, scopeError);
        }

        var type = request.PostType?.Trim() ?? string.Empty;
        var title = request.Title?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        if (!CommunityPostTypes.All.Contains(type) || title.Length is < 3 or > 200 || body.Length is < 1 or > 5000)
        {
            return CommunityOutcome<CommunityPostDto>.Failure(
                CommunityFailureKind.Validation, "Choose a valid post type and provide a title and message within the allowed lengths.");
        }

        var access = await repository.GetAccessAsync(userId, courseOfferingId, programOfferingId, isPrivileged, cancellationToken);
        if (!access.OfferingExists || !access.CanRead)
        {
            return CommunityOutcome<CommunityPostDto>.Failure(CommunityFailureKind.NotFound, "Community not found.");
        }

        if (!access.CanPost || (type.Equals(CommunityPostTypes.Announcement, StringComparison.OrdinalIgnoreCase) && !access.CanAnnounce))
        {
            return CommunityOutcome<CommunityPostDto>.Failure(CommunityFailureKind.Forbidden, "You cannot create this post.");
        }

        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            CourseOfferingId = courseOfferingId,
            ProgramOfferingId = programOfferingId,
            AuthorUserId = userId,
            PostType = type.Equals(CommunityPostTypes.Announcement, StringComparison.OrdinalIgnoreCase)
                ? CommunityPostTypes.Announcement : CommunityPostTypes.Discussion,
            Title = title,
            Body = body,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };
        return CommunityOutcome<CommunityPostDto>.Success(
            await repository.AddPostAsync(post, userId, cancellationToken));
    }

    public async Task<CommunityOutcome<CommunityReplyDto>> CreateReplyAsync(
        Guid userId, Guid postId, bool isPrivileged, CreateCommunityReplyRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length is < 1 or > 3000)
        {
            return CommunityOutcome<CommunityReplyDto>.Failure(CommunityFailureKind.Validation, "Reply is required and must be 3,000 characters or fewer.");
        }

        var post = await repository.FindPostAsync(postId, cancellationToken);
        if (post is null || post.DeletedAtUtc is not null)
        {
            return CommunityOutcome<CommunityReplyDto>.Failure(CommunityFailureKind.NotFound, "Post not found.");
        }

        var access = await repository.GetAccessAsync(
            userId, post.CourseOfferingId, post.ProgramOfferingId, isPrivileged, cancellationToken);
        if (!access.CanRead)
        {
            return CommunityOutcome<CommunityReplyDto>.Failure(CommunityFailureKind.NotFound, "Post not found.");
        }

        if (!access.CanPost)
        {
            return CommunityOutcome<CommunityReplyDto>.Failure(CommunityFailureKind.Forbidden, "You cannot reply in this community.");
        }

        if (post.IsLocked)
        {
            return CommunityOutcome<CommunityReplyDto>.Failure(CommunityFailureKind.Conflict, "This discussion is locked.");
        }

        var reply = new CommunityReply
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorUserId = userId,
            Body = body,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };
        return CommunityOutcome<CommunityReplyDto>.Success(
            await repository.AddReplyAsync(reply, userId, cancellationToken));
    }

    public async Task<CommunityOutcome<CommunityPostDto>> ModerateAsync(
        Guid userId, Guid postId, bool isPrivileged, ModerateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        var post = await repository.FindPostAsync(postId, cancellationToken);
        if (post is null || post.DeletedAtUtc is not null)
        {
            return CommunityOutcome<CommunityPostDto>.Failure(CommunityFailureKind.NotFound, "Post not found.");
        }

        var access = await repository.GetAccessAsync(
            userId, post.CourseOfferingId, post.ProgramOfferingId, isPrivileged, cancellationToken);
        if (!access.CanRead)
        {
            return CommunityOutcome<CommunityPostDto>.Failure(CommunityFailureKind.NotFound, "Post not found.");
        }

        if (!access.CanModerate)
        {
            return CommunityOutcome<CommunityPostDto>.Failure(CommunityFailureKind.Forbidden, "You cannot moderate this community.");
        }

        post.IsPinned = request.IsPinned;
        post.IsLocked = request.IsLocked;
        post.UpdatedAtUtc = timeProvider.GetUtcNow();
        return CommunityOutcome<CommunityPostDto>.Success(
            await repository.UpdateModerationAsync(post, userId, cancellationToken));
    }

    private static string? ValidateOfferingScope(Guid? courseOfferingId, Guid? programOfferingId)
        => courseOfferingId.HasValue == programOfferingId.HasValue
            ? "Choose exactly one course or program offering."
            : null;
}
