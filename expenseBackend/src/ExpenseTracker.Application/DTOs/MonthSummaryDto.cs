namespace ExpenseTracker.Application.DTOs;

// Month is "yyyy-MM". Difference = budget − spent (negative when over).
// Status is "Green" (spent ≤ budget), "Red" (spent > budget) or null without a budget.
public sealed record MonthSummaryDto(
    string Month,
    decimal? MonthlyBudget,
    decimal Spent,
    decimal? Difference,
    string? Status);
