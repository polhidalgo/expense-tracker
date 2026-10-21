using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.DTOs;

// Frequency is nullable so a missing value is rejected instead of becoming Weekly.
public sealed record FixedExpenseRequest(
    string? Name,
    decimal Amount,
    string? Currency,
    Guid CategoryId,
    DateOnly StartDate,
    FixedFrequency? Frequency);
