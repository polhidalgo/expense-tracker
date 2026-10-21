using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.DTOs;

public sealed record FixedExpenseDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    decimal Amount,
    string Currency,
    DateOnly StartDate,
    FixedFrequency Frequency,
    DateTimeOffset CreatedAt);
