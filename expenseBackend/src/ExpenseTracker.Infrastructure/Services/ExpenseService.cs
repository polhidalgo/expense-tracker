using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Rules;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services;

public sealed class ExpenseService(AppDbContext db, ICurrentUser currentUser) : IExpenseService
{
    public async Task<ExpenseDto> CreateAsync(ExpenseRequest request, CancellationToken cancellationToken = default)
    {
        // The entity validates the fields (400) before the category is looked up (404).
        var expense = new Expense(
            currentUser.UserId,
            request.CategoryId,
            request.Amount,
            request.Currency,
            request.ExpenseDate,
            request.Merchant,
            request.Description,
            ExpenseSource.Manual);

        var categoryName = await GetCategoryNameAsync(request.CategoryId, cancellationToken);

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(expense, categoryName);
    }

    public async Task<ExpenseDto> UpdateAsync(Guid id, ExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var expense = await db.Expenses
            .SingleOrDefaultAsync(e => e.Id == id && e.UserId == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Expense not found.");

        // Id, owner, source and creation time are never changed. Nothing is saved if anything below fails.
        expense.Update(
            request.CategoryId,
            request.Amount,
            request.Currency,
            request.ExpenseDate,
            request.Merchant,
            request.Description);

        var categoryName = await GetCategoryNameAsync(request.CategoryId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(expense, categoryName);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await db.Expenses
            .Where(e => e.Id == id && e.UserId == currentUser.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
            throw new NotFoundException("Expense not found.");
    }

    public async Task<ExpenseDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var found = await db.Expenses
            .AsNoTracking()
            .Where(e => e.Id == id && e.UserId == currentUser.UserId)
            .Join(db.Categories, e => e.CategoryId, c => c.Id, (e, c) => new { Expense = e, CategoryName = c.Name })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Expense not found.");

        return ToDto(found.Expense, found.CategoryName);
    }

    public async Task<PagedResult<ExpenseDto>> ListAsync(ExpenseListQuery query, CancellationToken cancellationToken = default)
    {
        Validate(query);

        var expenses = db.Expenses.AsNoTracking().Where(e => e.UserId == currentUser.UserId);

        if (query.StartDate is { } startDate)
            expenses = expenses.Where(e => e.ExpenseDate >= startDate);

        if (query.EndDate is { } endDate)
            expenses = expenses.Where(e => e.ExpenseDate <= endDate);

        if (query.CategoryId is { } categoryId)
            expenses = expenses.Where(e => e.CategoryId == categoryId);

        var totalCount = await expenses.CountAsync(cancellationToken);

        // Newest first, then by id so pages are stable. PostgreSQL does the filtering and paging.
        var page = await expenses
            .Join(db.Categories, e => e.CategoryId, c => c.Id, (e, c) => new { Expense = e, CategoryName = c.Name })
            .OrderByDescending(x => x.Expense.ExpenseDate)
            .ThenByDescending(x => x.Expense.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(x => ToDto(x.Expense, x.CategoryName)).ToList();

        return new PagedResult<ExpenseDto>(items, query.Page, query.PageSize, totalCount);
    }

    // Invalid values are rejected, never clamped (FR-EXP-003).
    private static void Validate(ExpenseListQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.Page < 1)
            errors["page"] = ["Page must be 1 or greater."];

        if (query.PageSize is < 1 or > ExpenseListQuery.MaxPageSize)
            errors["pageSize"] = [$"Page size must be between 1 and {ExpenseListQuery.MaxPageSize}."];

        if (query.StartDate > query.EndDate)
            errors["startDate"] = ["Start date must not be after end date."];

        if (errors.Count > 0)
            throw new DomainValidationException(errors);
    }

    private async Task<string> GetCategoryNameAsync(Guid categoryId, CancellationToken cancellationToken) =>
        await db.Categories
            .Where(c => c.Id == categoryId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Category not found.");

    internal static ExpenseDto ToDto(Expense e, string categoryName) => new(
        e.Id,
        e.UserId,
        e.CategoryId,
        categoryName,
        e.Amount,
        e.Currency,
        e.ExpenseDate,
        e.Merchant,
        e.Description,
        e.Source.ToString(),
        e.CreatedAt);
}
