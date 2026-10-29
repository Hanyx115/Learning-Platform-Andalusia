using System.Text.Json;
using AndalusiaAcademy.Application.Notifications;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AndalusiaAcademy.Infrastructure.Notifications;

public sealed class NotificationOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<NotificationOutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Notifications:ProcessingEnabled", true))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await QueueSessionRemindersAsync(stoppingToken);
                await ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification outbox processing failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), timeProvider, stoppingToken);
        }
    }

    public async Task<int> ProcessOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null &&
                              (message.NextAttemptAtUtc == null || message.NextAttemptAtUtc <= now))
            .OrderBy(message => message.OccurredAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var request = JsonSerializer.Deserialize<NotificationEventRequest>(message.PayloadJson)
                    ?? throw new InvalidOperationException("Notification outbox payload is invalid.");
                var template = await dbContext.NotificationTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(candidate => candidate.Code == request.TemplateCode && candidate.IsActive, cancellationToken);

                if (template is null)
                {
                    throw new InvalidOperationException($"Active notification template '{request.TemplateCode}' was not found.");
                }

                var validUserIds = await dbContext.Users
                    .AsNoTracking()
                    .Where(user => request.RecipientUserIds.Contains(user.Id) && user.IsActive)
                    .Select(user => user.Id)
                    .ToListAsync(cancellationToken);

                var existingUserIds = await dbContext.Notifications
                    .AsNoTracking()
                    .Where(notification => notification.SourceEventKey == message.EventKey && validUserIds.Contains(notification.UserId))
                    .Select(notification => notification.UserId)
                    .ToListAsync(cancellationToken);

                foreach (var userId in validUserIds.Except(existingUserIds))
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        TemplateId = template.Id,
                        EventType = request.TemplateCode,
                        SourceEventKey = message.EventKey,
                        TitleEn = Render(request.TitleEn ?? template.TitleEn, request.Tokens),
                        TitleAr = Render(request.TitleAr ?? template.TitleAr, request.Tokens),
                        MessageEn = Render(request.MessageEn ?? template.MessageEn, request.Tokens),
                        MessageAr = Render(request.MessageAr ?? template.MessageAr, request.Tokens),
                        LinkUrl = request.LinkUrl,
                        CreatedAtUtc = now,
                    };
                    notification.Deliveries.Add(new NotificationDelivery
                    {
                        Id = Guid.NewGuid(),
                        Channel = NotificationChannels.InApp,
                        Status = NotificationDeliveryStatuses.Delivered,
                        AttemptCount = 1,
                        DeliveredAtUtc = now,
                    });
                    dbContext.Notifications.Add(notification);
                }

                message.ProcessedAtUtc = now;
                message.LastError = null;
                message.NextAttemptAtUtc = null;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                message.AttemptCount++;
                message.LastError = exception.Message[..Math.Min(exception.Message.Length, 2000)];
                message.NextAttemptAtUtc = now.AddMinutes(Math.Min(60, Math.Pow(2, message.AttemptCount)));
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogWarning(exception, "Notification outbox message {MessageId} will be retried.", message.Id);
            }
        }

        return messages.Count;
    }

    private async Task QueueSessionRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        var reminderLimit = now.AddHours(24);

        var courseSessions = await dbContext.CourseSessions
            .AsNoTracking()
            .Where(session => session.Status != CourseSessionStatuses.Cancelled &&
                              session.StartsAtUtc > now && session.StartsAtUtc <= reminderLimit)
            .Select(session => new { session.Id, session.CourseOfferingId, session.Title, session.StartsAtUtc })
            .ToListAsync(cancellationToken);

        foreach (var session in courseSessions)
        {
            var learners = await dbContext.Enrollments.AsNoTracking()
                .Where(enrollment => enrollment.CourseOfferingId == session.CourseOfferingId && enrollment.Status == "Active")
                .Select(enrollment => enrollment.UserId)
                .ToListAsync(cancellationToken);
            await AddReminderOutboxAsync(dbContext, session.Id, session.Title, session.StartsAtUtc, learners, now, cancellationToken);
        }

        var programSessions = await dbContext.ProgramSessions
            .AsNoTracking()
            .Where(session => session.Status != CourseSessionStatuses.Cancelled &&
                              session.StartsAtUtc > now && session.StartsAtUtc <= reminderLimit)
            .Select(session => new { session.Id, session.ProgramOfferingId, session.Title, session.StartsAtUtc })
            .ToListAsync(cancellationToken);

        foreach (var session in programSessions)
        {
            var learners = await dbContext.Enrollments.AsNoTracking()
                .Where(enrollment => enrollment.ProgramOfferingId == session.ProgramOfferingId && enrollment.Status == "Active")
                .Select(enrollment => enrollment.UserId)
                .ToListAsync(cancellationToken);
            await AddReminderOutboxAsync(dbContext, session.Id, session.Title, session.StartsAtUtc, learners, now, cancellationToken);
        }
    }

    private static async Task AddReminderOutboxAsync(
        ApplicationDbContext dbContext,
        Guid sessionId,
        string title,
        DateTimeOffset startsAtUtc,
        IReadOnlyList<Guid> learners,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var learnerId in learners)
        {
            var eventKey = $"session-reminder:{sessionId:N}:{learnerId:N}";
            if (await dbContext.OutboxMessages.AsNoTracking().AnyAsync(message => message.EventKey == eventKey, cancellationToken))
            {
                continue;
            }

            var request = new NotificationEventRequest(
                eventKey,
                NotificationTemplateCodes.SessionReminder,
                [learnerId],
                new Dictionary<string, string>
                {
                    ["sessionTitle"] = title,
                    ["startsAt"] = startsAtUtc.ToString("u"),
                },
                "/profile/schedule");
            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventKey = eventKey,
                EventType = request.TemplateCode,
                PayloadJson = JsonSerializer.Serialize(request),
                OccurredAtUtc = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Render(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var result = template;
        foreach (var token in tokens)
        {
            result = result.Replace($"{{{{{token.Key}}}}}", token.Value, StringComparison.Ordinal);
        }

        return result;
    }
}
