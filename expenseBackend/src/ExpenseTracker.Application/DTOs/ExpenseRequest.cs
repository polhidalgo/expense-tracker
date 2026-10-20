namespace ExpenseTracker.Application.DTOs;

public sealed record ExpenseRequest(
    decimal Amount,
    string? Currency,
    DateOnly ExpenseDate,
    Guid CategoryId,
    string? Merchant,
    string? Description);
