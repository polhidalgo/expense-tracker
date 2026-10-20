namespace ExpenseTracker.Application.DTOs;

// The values the user reviewed, plus the analysis they came from.
public sealed record ConfirmReceiptRequest(
    Guid AnalysisId,
    decimal Amount,
    string? Currency,
    DateOnly ExpenseDate,
    Guid CategoryId,
    string? Merchant,
    string? Description)
{
    public ExpenseRequest ToExpenseRequest() =>
        new(Amount, Currency, ExpenseDate, CategoryId, Merchant, Description);
}
