using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services;

public sealed class DashboardService(AppDbContext db, ICurrentUser currentUser) : IDashboardService
{
    private const int RecentCount = 5;

    public async Task<DashboardSummaryDto> GetSummaryAsync(DashboardQuery query, CancellationToken cancellationToken = default)
    {
        var (startDate, endDate) = Validate(query);

        var expenses = db.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == currentUser.UserId && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate);

        var totals = await expenses
            .GroupBy(e => e.Currency)
            .OrderBy(g => g.Key)
            .Select(g => new CurrencyTotalDto(g.Key, g.Sum(e => e.Amount), g.Count()))
            .ToListAsync(cancellationToken);

        var byCategory = await expenses
            .Join(db.Categories, e => e.CategoryId, c => c.Id, (e, c) => new { e.CategoryId, CategoryName = c.Name, e.Currency, e.Amount })
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.Currency })
            .OrderByDescending(g => g.Sum(x => x.Amount))
            .Select(g => new CategoryTotalDto(g.Key.CategoryId, g.Key.CategoryName, g.Key.Currency, g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);

        var recent = await expenses
            .Join(db.Categories, e => e.CategoryId, c => c.Id, (e, c) => new { Expense = e, CategoryName = c.Name })
            .OrderByDescending(x => x.Expense.ExpenseDate)
            .ThenByDescending(x => x.Expense.CreatedAt)
            .Take(RecentCount)
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto(
            startDate,
            endDate,
            totals,
            byCategory,
            recent.Select(x => ExpenseService.ToDto(x.Expense, x.CategoryName)).ToList());
    }

    private static (DateOnly StartDate, DateOnly EndDate) Validate(DashboardQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.StartDate is null)
            errors["startDate"] = ["Start date is required."];

        if (query.EndDate is null)
            errors["endDate"] = ["End date is required."];

        if (query.StartDate > query.EndDate)
            errors["startDate"] = ["Start date must not be after end date."];

        if (errors.Count > 0)
            throw new DomainValidationException(errors);

        return (query.StartDate!.Value, query.EndDate!.Value);
    }
}
