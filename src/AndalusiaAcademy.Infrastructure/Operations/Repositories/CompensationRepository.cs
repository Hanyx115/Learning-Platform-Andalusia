using AndalusiaAcademy.Application.DTOs.Compensation;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Application.Repositories;
using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Operations.Repositories;

public sealed class CompensationRepository(ApplicationDbContext dbContext) : ICompensationRepository
{
    public async Task<BalanceDto> GetBalanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await dbContext.WalletAccounts.AsNoTracking()
            .Where(candidate => candidate.UserId == userId)
            .Select(candidate => new BalanceDto(candidate.Balance, candidate.Currency, candidate.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return account ?? new BalanceDto(0, "EGP", null);
    }

    public async Task<BalanceDto> AddDemoCreditAsync(
        Guid userId,
        decimal amount,
        string method,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.WalletAccounts.SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        if (account is null)
        {
            account = new WalletAccount { UserId = userId, Currency = "EGP" };
            dbContext.WalletAccounts.Add(account);
        }

        account.Balance += amount;
        account.UpdatedAtUtc = now;
        dbContext.WalletTransactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Currency = "EGP",
            Type = "DemoCredit",
            Reference = $"DEMO:{method.ToUpperInvariant()}:{Guid.NewGuid():N}",
            CreatedAtUtc = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BalanceDto(account.Balance, account.Currency, account.UpdatedAtUtc);
    }

    public async Task<IReadOnlyList<InstructorCompensationTargetDto>> GetInstructorTargetsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.CourseSessions.AsNoTracking()
            .Where(session => session.InstructorUserId != null)
            .Select(session => new
            {
                InstructorUserId = session.InstructorUserId!.Value,
                InstructorName = dbContext.Users.Where(user => user.Id == session.InstructorUserId)
                    .Select(user => user.FirstName + " " + user.LastName).Single(),
                session.CourseOfferingId,
                CourseTitle = session.CourseOffering!.Course!.Title,
                session.CourseOffering.StartsAtUtc,
                session.CourseOffering.EndsAtUtc,
                OfferingStatus = session.CourseOffering.Status,
            })
            .Distinct()
            .OrderBy(item => item.CourseTitle)
            .ThenBy(item => item.InstructorName)
            .ToListAsync(cancellationToken);

        var rates = await dbContext.InstructorCourseRates.AsNoTracking().ToListAsync(cancellationToken);
        var payouts = await dbContext.CompensationPayouts.AsNoTracking()
            .Where(payout => payout.InstructorCourseRateId != null)
            .ToDictionaryAsync(payout => payout.InstructorCourseRateId!.Value, payout => payout.Status, cancellationToken);

        var ratesByAssignment = rates.ToDictionary(rate => (rate.InstructorUserId, rate.CourseOfferingId));
        return assignments.Select(item =>
        {
            ratesByAssignment.TryGetValue((item.InstructorUserId, item.CourseOfferingId), out var rate);
            var eligible = item.OfferingStatus == "Cancelled"
                ? item.StartsAtUtc <= now
                : item.EndsAtUtc <= now;
            return new InstructorCompensationTargetDto(
                rate?.Id,
                item.InstructorUserId,
                item.InstructorName,
                item.CourseOfferingId,
                item.CourseTitle,
                item.StartsAtUtc,
                item.EndsAtUtc,
                item.OfferingStatus,
                rate?.Amount,
                rate?.Currency ?? "EGP",
                eligible,
                rate is not null && payouts.TryGetValue(rate.Id, out var payoutStatus) ? payoutStatus : null);
        }).ToList();
    }

    public async Task<bool> SetInstructorRateAsync(
        SetInstructorCourseRateRequest request,
        Guid managerUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var isAssigned = await dbContext.CourseSessions.AnyAsync(
            session => session.CourseOfferingId == request.CourseOfferingId
                && session.InstructorUserId == request.InstructorUserId,
            cancellationToken);
        if (!isAssigned)
        {
            return false;
        }

        var rate = await dbContext.InstructorCourseRates.SingleOrDefaultAsync(
            candidate => candidate.CourseOfferingId == request.CourseOfferingId
                && candidate.InstructorUserId == request.InstructorUserId,
            cancellationToken);
        if (rate is null)
        {
            rate = new InstructorCourseRate
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = request.CourseOfferingId,
                InstructorUserId = request.InstructorUserId,
            };
            dbContext.InstructorCourseRates.Add(rate);
        }

        var hasPayout = await dbContext.CompensationPayouts.AnyAsync(
            payout => payout.InstructorCourseRateId == rate.Id,
            cancellationToken);
        if (hasPayout)
        {
            throw new InvalidOperationException("A rate cannot be changed after its payout has been created.");
        }

        rate.Amount = decimal.Round(request.Amount, 2);
        rate.Currency = request.Currency;
        rate.SetByManagerUserId = managerUserId;
        rate.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StaffCompensationRateDto>> GetStaffRatesAsync(CancellationToken cancellationToken)
    {
        var roleRows = await (
            from user in dbContext.Users.AsNoTracking()
            join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == RoleNames.Manager || role.Name == RoleNames.ContentManager
            select new { user.Id, Name = user.FirstName + " " + user.LastName, Role = role.Name! })
            .ToListAsync(cancellationToken);
        var rates = await dbContext.StaffCompensationRates.AsNoTracking()
            .ToDictionaryAsync(rate => rate.UserId, cancellationToken);

        return roleRows.GroupBy(row => new { row.Id, row.Name })
            .Select(group =>
            {
                rates.TryGetValue(group.Key.Id, out var rate);
                return new StaffCompensationRateDto(
                    group.Key.Id,
                    group.Key.Name,
                    string.Join(", ", group.Select(item => item.Role).OrderBy(role => role)),
                    rate?.MonthlyAmount,
                    rate?.Currency ?? "EGP");
            })
            .OrderBy(item => item.StaffName)
            .ToList();
    }

    public async Task<bool> SetStaffRateAsync(
        Guid userId,
        decimal amount,
        string currency,
        Guid adminUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var allowedRoleIds = await dbContext.Roles
            .Where(role => role.Name == RoleNames.Manager || role.Name == RoleNames.ContentManager)
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);
        var isEligible = await dbContext.UserRoles.AnyAsync(
            userRole => userRole.UserId == userId && allowedRoleIds.Contains(userRole.RoleId),
            cancellationToken);
        if (!isEligible)
        {
            return false;
        }

        var rate = await dbContext.StaffCompensationRates.SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        if (rate is null)
        {
            rate = new StaffCompensationRate { UserId = userId };
            dbContext.StaffCompensationRates.Add(rate);
        }

        rate.MonthlyAmount = amount;
        rate.Currency = currency;
        rate.SetByAdminUserId = adminUserId;
        rate.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CompensationPayoutDto>> GetPayoutsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.CompensationPayouts.AsNoTracking()
            .OrderByDescending(payout => payout.CreatedAtUtc)
            .Select(payout => new CompensationPayoutDto(
                payout.Id,
                payout.UserId,
                dbContext.Users.Where(user => user.Id == payout.UserId)
                    .Select(user => user.FirstName + " " + user.LastName).Single(),
                payout.PayoutType,
                payout.Reason,
                payout.Amount,
                payout.Currency,
                payout.Status,
                payout.CreatedAtUtc,
                payout.ReviewedAtUtc,
                payout.ApprovedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CompensationActionOutcome> CreateInstructorPayoutAsync(
        Guid rateId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rate = await dbContext.InstructorCourseRates
            .Include(item => item.CourseOffering).ThenInclude(offering => offering!.Course)
            .SingleOrDefaultAsync(item => item.Id == rateId, cancellationToken);
        if (rate is null)
        {
            return CompensationActionOutcome.Failure("Instructor course rate not found.");
        }

        if (await dbContext.CompensationPayouts.AnyAsync(payout => payout.InstructorCourseRateId == rateId, cancellationToken))
        {
            return CompensationActionOutcome.Failure("A payout already exists for this course assignment.");
        }

        var offering = rate.CourseOffering!;
        var isLateCancellation = offering.Status == "Cancelled" && offering.StartsAtUtc <= now;
        var isCompleted = offering.Status != "Cancelled" && offering.EndsAtUtc <= now;
        if (!isLateCancellation && !isCompleted)
        {
            return CompensationActionOutcome.Failure(
                offering.Status == "Cancelled"
                    ? "A course cancelled before it started is unpaid."
                    : "The course must finish before its payout can be created.");
        }

        var payout = new CompensationPayout
        {
            Id = Guid.NewGuid(),
            UserId = rate.InstructorUserId,
            PayoutType = CompensationPayoutType.InstructorCourse,
            InstructorCourseRateId = rate.Id,
            Amount = rate.Amount,
            Currency = rate.Currency,
            Status = CompensationPayoutStatus.Draft,
            Reason = isLateCancellation
                ? $"Late academy cancellation: {offering.Course!.Title}"
                : $"Completed course: {offering.Course!.Title}",
            CreatedAtUtc = now,
        };
        dbContext.CompensationPayouts.Add(payout);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CompensationActionOutcome.Success(await ToDtoAsync(payout, cancellationToken));
    }

    public async Task<CompensationActionOutcome> CreateStaffPayoutAsync(
        Guid userId,
        DateOnly periodMonth,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var currentMonth = new DateOnly(now.Year, now.Month, 1);
        if (periodMonth > currentMonth)
        {
            return CompensationActionOutcome.Failure("A monthly payout cannot be created for a future month.");
        }

        var rate = await dbContext.StaffCompensationRates.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        if (rate is null)
        {
            return CompensationActionOutcome.Failure("Set this staff member's monthly rate first.");
        }

        if (await dbContext.CompensationPayouts.AnyAsync(
            payout => payout.UserId == userId && payout.PeriodMonth == periodMonth
                && payout.PayoutType == CompensationPayoutType.StaffMonthly,
            cancellationToken))
        {
            return CompensationActionOutcome.Failure("A payout already exists for this staff member and month.");
        }

        var payout = new CompensationPayout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PayoutType = CompensationPayoutType.StaffMonthly,
            PeriodMonth = periodMonth,
            Amount = rate.MonthlyAmount,
            Currency = rate.Currency,
            Status = CompensationPayoutStatus.Draft,
            Reason = $"Monthly compensation: {periodMonth:yyyy-MM}",
            CreatedAtUtc = now,
        };
        dbContext.CompensationPayouts.Add(payout);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CompensationActionOutcome.Success(await ToDtoAsync(payout, cancellationToken));
    }

    public async Task<CompensationActionOutcome> ReviewPayoutAsync(
        Guid payoutId,
        Guid managerUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var payout = await dbContext.CompensationPayouts.SingleOrDefaultAsync(item => item.Id == payoutId, cancellationToken);
        if (payout is null)
        {
            return CompensationActionOutcome.Failure("Payout not found.");
        }

        if (payout.Status != CompensationPayoutStatus.Draft)
        {
            return CompensationActionOutcome.Failure("Only a draft payout can be reviewed.");
        }

        if (payout.UserId == managerUserId)
        {
            return CompensationActionOutcome.Failure("A manager cannot review their own payout.");
        }

        payout.Status = CompensationPayoutStatus.Reviewed;
        payout.ReviewedByManagerUserId = managerUserId;
        payout.ReviewedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CompensationActionOutcome.Success(await ToDtoAsync(payout, cancellationToken));
    }

    public async Task<CompensationActionOutcome> ApprovePayoutAsync(
        Guid payoutId,
        Guid adminUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var payout = await dbContext.CompensationPayouts.SingleOrDefaultAsync(item => item.Id == payoutId, cancellationToken);
        if (payout is null)
        {
            return CompensationActionOutcome.Failure("Payout not found.");
        }

        if (payout.Status != CompensationPayoutStatus.Reviewed)
        {
            return CompensationActionOutcome.Failure("The payout must be reviewed before it can be approved.");
        }

        if (payout.ReviewedByManagerUserId == adminUserId)
        {
            return CompensationActionOutcome.Failure("The reviewer and approving administrator must be different users.");
        }

        var account = await dbContext.WalletAccounts.SingleOrDefaultAsync(item => item.UserId == payout.UserId, cancellationToken);
        if (account is null)
        {
            account = new WalletAccount { UserId = payout.UserId, Currency = payout.Currency };
            dbContext.WalletAccounts.Add(account);
        }
        else if (account.Currency != payout.Currency)
        {
            return CompensationActionOutcome.Failure("The payout currency does not match the user's balance currency.");
        }

        payout.Status = CompensationPayoutStatus.Paid;
        payout.ApprovedByAdminUserId = adminUserId;
        payout.ApprovedAtUtc = now;
        account.Balance += payout.Amount;
        account.UpdatedAtUtc = now;
        dbContext.WalletTransactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            UserId = payout.UserId,
            Amount = payout.Amount,
            Currency = payout.Currency,
            Type = "CompensationPayout",
            Reference = $"PAYOUT:{payout.Id:N}",
            CreatedAtUtc = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return CompensationActionOutcome.Success(await ToDtoAsync(payout, cancellationToken));
    }

    private async Task<CompensationPayoutDto> ToDtoAsync(CompensationPayout payout, CancellationToken cancellationToken)
    {
        var payeeName = await dbContext.Users.AsNoTracking().Where(user => user.Id == payout.UserId)
            .Select(user => user.FirstName + " " + user.LastName)
            .SingleAsync(cancellationToken);
        return new CompensationPayoutDto(
            payout.Id, payout.UserId, payeeName, payout.PayoutType, payout.Reason,
            payout.Amount, payout.Currency, payout.Status, payout.CreatedAtUtc,
            payout.ReviewedAtUtc, payout.ApprovedAtUtc);
    }
}
