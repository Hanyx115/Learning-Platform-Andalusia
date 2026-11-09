using AndalusiaAcademy.Application.DTOs.Community;
using AndalusiaAcademy.Application.Enrollment;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Community.Repositories;

public sealed class CommunityRepository(ApplicationDbContext dbContext) : ICommunityRepository
{
    public async Task<CommunityAccessDto> GetAccessAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, bool isPrivileged, CancellationToken cancellationToken)
    {
        var title = courseOfferingId.HasValue
            ? await dbContext.CourseOfferings.AsNoTracking().Where(item => item.Id == courseOfferingId)
                .Select(item => item.Course!.Title).SingleOrDefaultAsync(cancellationToken)
            : await dbContext.ProgramOfferings.AsNoTracking().Where(item => item.Id == programOfferingId)
                .Select(item => item.Program!.Title).SingleOrDefaultAsync(cancellationToken);
        if (title is null)
        {
            return new CommunityAccessDto(false, false, false, false, false, string.Empty);
        }

        var isEnrolled = await dbContext.Enrollments.AsNoTracking().AnyAsync(enrollment =>
            enrollment.UserId == userId && enrollment.Status == EnrollmentStatus.Active
            && enrollment.CourseOfferingId == courseOfferingId && enrollment.ProgramOfferingId == programOfferingId,
            cancellationToken);
        var isAssignedInstructor = courseOfferingId.HasValue
            ? await dbContext.CourseSessions.AsNoTracking().AnyAsync(session =>
                session.CourseOfferingId == courseOfferingId && session.InstructorUserId == userId, cancellationToken)
            : await dbContext.ProgramSessions.AsNoTracking().AnyAsync(session =>
                session.ProgramOfferingId == programOfferingId && session.InstructorUserId == userId, cancellationToken);
        var canRead = isPrivileged || isEnrolled || isAssignedInstructor;
        var canLead = isPrivileged || isAssignedInstructor;
        return new CommunityAccessDto(true, canRead, canRead, canLead, canLead, title);
    }

    public async Task<CommunityPageDto> GetPageAsync(
        Guid userId, Guid? courseOfferingId, Guid? programOfferingId, CommunityAccessDto access,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.CommunityPosts.AsNoTracking()
            .Where(post => post.DeletedAtUtc == null
                && post.CourseOfferingId == courseOfferingId
                && post.ProgramOfferingId == programOfferingId);
        var totalCount = await query.CountAsync(cancellationToken);
        var posts = await query
            .OrderByDescending(post => post.IsPinned)
            .ThenByDescending(post => post.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var postIds = posts.Select(post => post.Id).ToList();
        var replies = await dbContext.CommunityReplies.AsNoTracking()
            .Where(reply => postIds.Contains(reply.PostId) && reply.DeletedAtUtc == null)
            .OrderBy(reply => reply.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var userIds = posts.Select(post => post.AuthorUserId)
            .Concat(replies.Select(reply => reply.AuthorUserId)).Distinct().ToList();
        var names = await dbContext.Users.AsNoTracking().Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => (user.FirstName + " " + user.LastName).Trim(), cancellationToken);
        var items = posts.Select(post => ToDto(post, userId, names, replies.Where(reply => reply.PostId == post.Id))).ToList();
        return new CommunityPageDto(
            access.OfferingTitle, courseOfferingId, programOfferingId,
            access.CanPost, access.CanAnnounce, access.CanModerate,
            page, pageSize, totalCount, items);
    }

    public Task<CommunityPost?> FindPostAsync(Guid postId, CancellationToken cancellationToken)
        => dbContext.CommunityPosts.SingleOrDefaultAsync(post => post.Id == postId, cancellationToken);

    public async Task<CommunityPostDto> AddPostAsync(
        CommunityPost post, Guid callerUserId, CancellationToken cancellationToken)
    {
        dbContext.CommunityPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        var authorName = await GetUserNameAsync(post.AuthorUserId, cancellationToken);
        return ToDto(post, callerUserId, new Dictionary<Guid, string> { [post.AuthorUserId] = authorName }, []);
    }

    public async Task<CommunityReplyDto> AddReplyAsync(
        CommunityReply reply, Guid callerUserId, CancellationToken cancellationToken)
    {
        dbContext.CommunityReplies.Add(reply);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CommunityReplyDto(
            reply.Id, reply.AuthorUserId, await GetUserNameAsync(reply.AuthorUserId, cancellationToken),
            reply.AuthorUserId == callerUserId, reply.Body, reply.CreatedAtUtc, reply.UpdatedAtUtc);
    }

    public async Task<CommunityPostDto> UpdateModerationAsync(
        CommunityPost post, Guid callerUserId, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        var replies = await dbContext.CommunityReplies.AsNoTracking()
            .Where(reply => reply.PostId == post.Id && reply.DeletedAtUtc == null)
            .OrderBy(reply => reply.CreatedAtUtc).ToListAsync(cancellationToken);
        var userIds = replies.Select(reply => reply.AuthorUserId).Append(post.AuthorUserId).Distinct().ToList();
        var names = await dbContext.Users.AsNoTracking().Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => (user.FirstName + " " + user.LastName).Trim(), cancellationToken);
        return ToDto(post, callerUserId, names, replies);
    }

    private async Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.Users.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => (user.FirstName + " " + user.LastName).Trim())
            .SingleAsync(cancellationToken);

    private static CommunityPostDto ToDto(
        CommunityPost post, Guid callerUserId, IReadOnlyDictionary<Guid, string> names, IEnumerable<CommunityReply> replies)
        => new(
            post.Id, post.PostType, post.Title, post.Body, post.AuthorUserId,
            names.GetValueOrDefault(post.AuthorUserId, "Academy member"),
            post.AuthorUserId == callerUserId, post.IsPinned, post.IsLocked,
            post.CreatedAtUtc, post.UpdatedAtUtc,
            replies.Select(reply => new CommunityReplyDto(
                reply.Id, reply.AuthorUserId, names.GetValueOrDefault(reply.AuthorUserId, "Academy member"),
                reply.AuthorUserId == callerUserId, reply.Body, reply.CreatedAtUtc, reply.UpdatedAtUtc)).ToList());
}
