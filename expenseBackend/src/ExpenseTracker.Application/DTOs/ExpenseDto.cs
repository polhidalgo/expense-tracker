namespace ExpenseTracker.Application.DTOs;

public sealed record ExpenseDto(
    Guid Id,
    Guid UserId,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string? Merchant,
    string? Description,
    DateTimeOffset CreatedAt);
