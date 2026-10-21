using System.Globalization;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services;

// All amounts are AUD (D-02), so totals are not grouped by currency.
public sealed class SummaryService(AppDbContext db, ICurrentUser currentUser) : ISummaryService
{
    public async Task<WeekSummaryDto> GetWeekAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        if (date is null)
            throw new DomainValidationException(new() { ["date"] = ["Date is required."] });

        // Monday to Sunday, by expense date.
        var weekStart = date.Value.AddDays(-(((int)date.Value.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(6);
        var userId = currentUser.UserId;

        var expenseTotals = await db.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.ExpenseDate >= weekStart && e.ExpenseDate <= weekEnd)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);

        // A user has few fixed expenses, so their payments are calculated in memory.
        var fixedExpenses = await db.FixedExpenses
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.StartDate <= weekEnd)
            .ToListAsync(cancellationToken);

        var categoryNames = await db.Categories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var weeklyBudget = await db.Budgets
            .Where(b => b.UserId == userId)
            .Select(b => (decimal?)b.WeeklyAmount)
            .SingleOrDefaultAsync(cancellationToken);

        var fixedPayments = fixedExpenses
            .SelectMany(f => f.PaymentsBetween(weekStart, weekEnd)
                .Select(day => new { f.CategoryId, Payment = new FixedPaymentDto(f.Id, f.Name, categoryNames[f.CategoryId], day, f.Amount) }))
            .OrderBy(x => x.Payment.Date)
            .ThenBy(x => x.Payment.Name)
            .ToList();

        var byCategory = expenseTotals
            .Concat(fixedPayments.Select(x => new { x.CategoryId, Total = x.Payment.Amount }))
            .GroupBy(x => x.CategoryId)
            .Select(g => new CategoryTotalDto(g.Key, categoryNames[g.Key], g.Sum(x => x.Total)))
            .OrderByDescending(c => c.Total)
            .ThenBy(c => c.CategoryName)
            .ToList();

        var spent = byCategory.Sum(c => c.Total);
        var remaining = weeklyBudget - spent;

        // Rounded to one decimal; never below 0 when over budget.
        decimal? percentLeft = weeklyBudget is { } budget
            ? Math.Max(0m, decimal.Round(remaining!.Value / budget * 100, 1, MidpointRounding.AwayFromZero))
            : null;

        return new WeekSummaryDto(
            weekStart,
            weekEnd,
            weeklyBudget,
            spent,
            remaining,
            percentLeft,
            byCategory,
            fixedPayments.Select(x => x.Payment).ToList());
    }

    public async Task<IReadOnlyList<MonthSummaryDto>> GetMonthsAsync(string? endMonth, CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParseExact(endMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastMonth))
            throw new DomainValidationException(new() { ["endMonth"] = ["End month is required, as yyyy-MM."] });

        var from = lastMonth.AddMonths(-11);
        var to = lastMonth.AddMonths(1).AddDays(-1);
        var userId = currentUser.UserId;

        var expenseTotals = await db.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);

        var fixedExpenses = await db.FixedExpenses
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.StartDate <= to)
            .ToListAsync(cancellationToken);

        var weeklyBudget = await db.Budgets
            .Where(b => b.UserId == userId)
            .Select(b => (decimal?)b.WeeklyAmount)
            .SingleOrDefaultAsync(cancellationToken);

        var months = new List<MonthSummaryDto>();
        for (var month = from; month <= lastMonth; month = month.AddMonths(1))
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);

            var spent = expenseTotals
                .Where(t => t.Year == month.Year && t.Month == month.Month)
                .Sum(t => t.Total)
                + fixedExpenses.Sum(f => f.PaymentsBetween(month, monthEnd).Count() * f.Amount);

            decimal? monthlyBudget = weeklyBudget is { } weekly
                ? Budget.MonthlyAmount(weekly, month.Year, month.Month)
                : null;

            string? status = monthlyBudget is null ? null : spent <= monthlyBudget ? "Green" : "Red";

            months.Add(new MonthSummaryDto(month.ToString("yyyy-MM", CultureInfo.InvariantCulture), monthlyBudget, spent, monthlyBudget - spent, status));
        }

        return months;
    }
}
