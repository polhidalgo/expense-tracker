namespace ExpenseTracker.Application.DTOs;

// Body for creating or updating an expense. Owner, source and creation time are never accepted from the client.
public sealed record ExpenseRequest(
    decimal Amount,
    string? Currency,
    DateOnly ExpenseDate,
    Guid CategoryId,
    string? Merchant,
    string? Description);
