using AndalusiaAcademy.Application.DTOs.Community;
using AndalusiaAcademy.Domain.Models;

namespace AndalusiaAcademy.Application.Repositories;

public interface ICommunityRepository
{
    Task<CommunityAccessDto> GetAccessAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, bool isPrivileged, CancellationToken cancellationToken);

    Task<CommunityPageDto> GetPageAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, CommunityAccessDto access,
        int page, int pageSize, CancellationToken cancellationToken);

    Task<CommunityPost?> FindPostAsync(Guid postId, CancellationToken cancellationToken);

    Task<CommunityPostDto> AddPostAsync(CommunityPost post, Guid callerUserId, CancellationToken cancellationToken);

    Task<CommunityReplyDto> AddReplyAsync(CommunityReply reply, Guid callerUserId, CancellationToken cancellationToken);

    Task<CommunityPostDto> UpdateModerationAsync(
        CommunityPost post, Guid callerUserId, CancellationToken cancellationToken);
}
