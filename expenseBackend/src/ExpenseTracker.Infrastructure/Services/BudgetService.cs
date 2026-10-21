using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services;

public sealed class BudgetService(AppDbContext db, ICurrentUser currentUser) : IBudgetService
{
    public async Task<BudgetDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var weeklyAmount = await db.Budgets
            .Where(b => b.UserId == currentUser.UserId)
            .Select(b => (decimal?)b.WeeklyAmount)
            .SingleOrDefaultAsync(cancellationToken);

        return new BudgetDto(weeklyAmount);
    }

    public async Task<BudgetDto> SetAsync(BudgetRequest request, CancellationToken cancellationToken = default)
    {
        var budget = await db.Budgets
            .SingleOrDefaultAsync(b => b.UserId == currentUser.UserId, cancellationToken);

        if (budget is null)
            db.Budgets.Add(new Budget(currentUser.UserId, request.WeeklyAmount));
        else
            budget.SetWeeklyAmount(request.WeeklyAmount);

        await db.SaveChangesAsync(cancellationToken);

        return new BudgetDto(request.WeeklyAmount);
    }

    // Removing a budget that does not exist is not an error.
    public Task DeleteAsync(CancellationToken cancellationToken = default) =>
        db.Budgets
            .Where(b => b.UserId == currentUser.UserId)
            .ExecuteDeleteAsync(cancellationToken);
}
