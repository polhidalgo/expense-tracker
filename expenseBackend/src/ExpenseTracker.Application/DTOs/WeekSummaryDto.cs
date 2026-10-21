namespace ExpenseTracker.Application.DTOs;

// Budget, Remaining and PercentLeft are null when the user has no budget.
public sealed record WeekSummaryDto(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    decimal? WeeklyBudget,
    decimal Spent,
    decimal? Remaining,
    decimal? PercentLeft,
    IReadOnlyList<CategoryTotalDto> ByCategory,
    IReadOnlyList<FixedPaymentDto> FixedPayments);

// Expenses and fixed payments of the category together.
public sealed record CategoryTotalDto(Guid CategoryId, string CategoryName, decimal Total);

public sealed record FixedPaymentDto(Guid FixedExpenseId, string Name, string CategoryName, DateOnly Date, decimal Amount);
