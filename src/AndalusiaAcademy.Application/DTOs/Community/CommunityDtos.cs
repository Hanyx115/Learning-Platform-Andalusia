namespace AndalusiaAcademy.Application.DTOs.Community;

public sealed record CommunityPageDto(
    string OfferingTitle,
    Guid? CourseOfferingId,
    Guid? ProgramOfferingId,
    bool CanPost,
    bool CanAnnounce,
    bool CanModerate,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<CommunityPostDto> Items);

public sealed record CommunityPostDto(
    Guid Id,
    string PostType,
    string Title,
    string Body,
    Guid AuthorUserId,
    string AuthorName,
    bool IsMine,
    bool IsPinned,
    bool IsLocked,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<CommunityReplyDto> Replies);

public sealed record CommunityReplyDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorName,
    bool IsMine,
    string Body,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CommunityAccessDto(
    bool OfferingExists,
    bool CanRead,
    bool CanPost,
    bool CanAnnounce,
    bool CanModerate,
    string OfferingTitle);

public sealed record CreateCommunityPostRequest(string PostType, string Title, string Body);

public sealed record CreateCommunityReplyRequest(string Body);

public sealed record ModerateCommunityPostRequest(bool IsPinned, bool IsLocked);

public enum CommunityFailureKind
{
    None,
    NotFound,
    Forbidden,
    Validation,
    Conflict,
}

public sealed record CommunityOutcome<T>(bool Succeeded, T? Value, CommunityFailureKind FailureKind, string? Error)
{
    public static CommunityOutcome<T> Success(T value) => new(true, value, CommunityFailureKind.None, null);

    public static CommunityOutcome<T> Failure(CommunityFailureKind kind, string error) => new(false, default, kind, error);
}
