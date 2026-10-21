namespace ExpenseTracker.Application.DTOs;

// WeeklyAmount is null when the user has no budget.
public sealed record BudgetDto(decimal? WeeklyAmount);
