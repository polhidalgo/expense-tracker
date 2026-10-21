using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services;

public sealed class FixedExpenseService(AppDbContext db, ICurrentUser currentUser) : IFixedExpenseService
{
    // A user has few fixed expenses, so there is no paging.
    public async Task<IReadOnlyList<FixedExpenseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.FixedExpenses
            .AsNoTracking()
            .Where(f => f.UserId == currentUser.UserId)
            .Join(db.Categories, f => f.CategoryId, c => c.Id, (f, c) => new { Fixed = f, CategoryName = c.Name })
            .OrderBy(x => x.Fixed.Name)
            .ThenBy(x => x.Fixed.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(x => ToDto(x.Fixed, x.CategoryName)).ToList();
    }

    public async Task<FixedExpenseDto> CreateAsync(FixedExpenseRequest request, CancellationToken cancellationToken = default)
    {
        // The entity validates the fields (400) before the category is looked up (404).
        var fixedExpense = new FixedExpense(
            currentUser.UserId,
            request.CategoryId,
            request.Name,
            request.Amount,
            request.Currency,
            request.StartDate,
            request.Frequency);

        var categoryName = await GetCategoryNameAsync(request.CategoryId, cancellationToken);

        db.FixedExpenses.Add(fixedExpense);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(fixedExpense, categoryName);
    }

    public async Task<FixedExpenseDto> UpdateAsync(Guid id, FixedExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var fixedExpense = await db.FixedExpenses
            .SingleOrDefaultAsync(f => f.Id == id && f.UserId == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Fixed expense not found.");

        // Nothing is saved if anything below fails.
        fixedExpense.Update(
            request.CategoryId,
            request.Name,
            request.Amount,
            request.Currency,
            request.StartDate,
            request.Frequency);

        var categoryName = await GetCategoryNameAsync(request.CategoryId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(fixedExpense, categoryName);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await db.FixedExpenses
            .Where(f => f.Id == id && f.UserId == currentUser.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
            throw new NotFoundException("Fixed expense not found.");
    }

    private async Task<string> GetCategoryNameAsync(Guid categoryId, CancellationToken cancellationToken) =>
        await db.Categories
            .Where(c => c.Id == categoryId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Category not found.");

    private static FixedExpenseDto ToDto(FixedExpense f, string categoryName) => new(
        f.Id,
        f.CategoryId,
        categoryName,
        f.Name,
        f.Amount,
        f.Currency,
        f.StartDate,
        f.Frequency,
        f.CreatedAt);
}
