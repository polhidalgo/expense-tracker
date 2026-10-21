namespace ExpenseTracker.Application.DTOs;

// Month is "yyyy-MM". Difference = budget − spent (negative when over).
// Status is "Green" (spent ≤ budget), "Red" (spent > budget) or null without a budget.
// Difference and Status are also null when nothing was spent in the month.
public sealed record MonthSummaryDto(
    string Month,
    decimal? MonthlyBudget,
    decimal Spent,
    decimal? Difference,
    string? Status);
